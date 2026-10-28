using System.Net;
using MediatR;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Localization;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.MailSettings.Commands.SendTestEmail;

public class SendTestEmailCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IMailDispatcher mailDispatcher
) : IRequestHandler<SendTestEmailCommand, Result> {
    public async Task<Result> Handle(SendTestEmailCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result.Failure("Only an employer can send a test email.", ResultErrorType.Forbidden);
        }

        var settings = await unitOfWork.MailSettings.GetForCurrentTenantAsync(cancellationToken);
        if (settings is null) {
            return Result.Failure("Save the mail settings before sending a test email.");
        }

        var employer = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (employer is null) {
            return Result.Failure("Your account was not found.", ResultErrorType.NotFound);
        }

        var provider = settings.Provider == MailProvider.MicrosoftGraph ? "Microsoft Graph" : "SMTP";
        var texts = EmailTexts.For(employer.Language);
        var greeting = texts.Get("greeting", ("firstName", employer.FirstName));
        var body = texts.Get("testEmail.body", ("provider", provider));
        var message = new EmailMessage(
            new[] { employer.Email },
            texts.Get("testEmail.subject"),
            $"<p>{WebUtility.HtmlEncode(greeting)}</p><p>{WebUtility.HtmlEncode(body)}</p>",
            $"{greeting}\n\n{body}");

        try {
            await mailDispatcher.DispatchAsync(settings, message, cancellationToken);
        } catch (EmailDeliveryException ex) {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }
}
