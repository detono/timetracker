using System.Net;
using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Localization;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler(
    IUnitOfWork unitOfWork,
    ITenantScope tenantScope,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IDateTimeProvider clock
) : IRequestHandler<RequestPasswordResetCommand, Result> {
    /// <summary>Limits how often one account can be sent a code, so nobody's mailbox gets flooded.</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken) {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        // Platform admins have no company, so no email to send with.
        if (user is not { IsActive: true, TenantId: { } tenantId }) {
            return Result.Success();
        }

        var tenant = await unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is not { IsActive: true }) {
            return Result.Success();
        }

        using var scope = tenantScope.Enter(tenantId);

        var settings = await unitOfWork.MailSettings.GetForCurrentTenantAsync(cancellationToken);
        if (settings is not { IsEnabled: true }) {
            return Result.Success();
        }

        var now = clock.UtcNow;
        var latest = await unitOfWork.PasswordResetCodes.GetLatestForUserAsync(user.Id, cancellationToken);
        if (latest is not null && now - latest.CreatedAtUtc < Cooldown) {
            return Result.Success();
        }

        // Only the newest code works.
        var code = PasswordResetCodes.Generate();
        await unitOfWork.PasswordResetCodes.RemoveAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.PasswordResetCodes.AddAsync(
            PasswordResetCode.Create(user.Id, passwordHasher.Hash(PasswordResetCodes.Normalize(code)), now),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var texts = EmailTexts.For(user.Language);
        var paragraphs = new[] {
            texts.Get("greeting", ("firstName", user.FirstName)),
            texts.Get("passwordReset.body", ("email", user.Email)),
            code,
            texts.Get("passwordReset.expiry", ("minutes", ((int)PasswordResetCode.Lifetime.TotalMinutes).ToString(texts.Culture))),
            texts.Get("passwordReset.ignore")
        };

        var html = string.Concat(paragraphs.Select(p => p == code
            ? $"<p style=\"font-size:24px;font-weight:bold;letter-spacing:4px;font-family:monospace\">{code}</p>"
            : $"<p>{WebUtility.HtmlEncode(p)}</p>"));

        // Delivery problems are logged by the sender; the response stays the same either way.
        await emailSender.SendAsync(new EmailMessage(
            new[] { user.Email },
            texts.Get("passwordReset.subject"),
            html,
            string.Join("\n\n", paragraphs)), cancellationToken);

        return Result.Success();
    }
}
