using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.MailSettings.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.MailSettings.Commands.UpdateMailSettings;

public class UpdateMailSettingsCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ISecretProtector secretProtector
) : IRequestHandler<UpdateMailSettingsCommand, Result<MailSettingsDto>> {
    public async Task<Result<MailSettingsDto>> Handle(UpdateMailSettingsCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<MailSettingsDto>.Failure("Only an employer can change mail settings.", ResultErrorType.Forbidden);
        }

        var settings = await unitOfWork.MailSettings.GetForCurrentTenantAsync(cancellationToken);
        var isNew = settings is null;
        settings ??= TenantMailSettings.Create(request.FromAddress, request.FromName);

        if (request.Provider == MailProvider.MicrosoftGraph) {
            if (string.IsNullOrWhiteSpace(request.GraphClientSecret) && settings.GraphClientSecretProtected is null) {
                return Result<MailSettingsDto>.Failure("A client secret is required.");
            }

            settings.UseMicrosoftGraph(request.GraphTenantId!, request.GraphClientId!, request.GraphMailbox!,
                ProtectIfGiven(request.GraphClientSecret));
        } else {
            settings.UseSmtp(request.SmtpHost!, request.SmtpPort, request.SmtpSecurity, request.SmtpUsername,
                ProtectIfGiven(request.SmtpPassword));
        }

        settings.SetSender(request.FromAddress, request.FromName);
        settings.SetEnabled(request.IsEnabled);

        if (isNew) {
            await unitOfWork.MailSettings.AddAsync(settings, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<MailSettingsDto>.Success(MailSettingsDto.From(settings));
    }

    // Secrets aren't trimmed: leading/trailing spaces can be part of a real password.
    private string? ProtectIfGiven(string? secret) =>
        string.IsNullOrEmpty(secret) ? null : secretProtector.Protect(secret);
}
