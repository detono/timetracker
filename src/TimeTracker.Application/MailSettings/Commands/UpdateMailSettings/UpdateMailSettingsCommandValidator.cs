using FluentValidation;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Application.MailSettings.Commands.UpdateMailSettings;

public class UpdateMailSettingsCommandValidator : AbstractValidator<UpdateMailSettingsCommand> {
    public UpdateMailSettingsCommandValidator() {
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.FromAddress).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.FromName).MaximumLength(200);

        When(x => x.Provider == MailProvider.Smtp, () => {
            RuleFor(x => x.SmtpHost).NotEmpty().MaximumLength(255);
            RuleFor(x => x.SmtpPort).InclusiveBetween(1, 65535);
            RuleFor(x => x.SmtpSecurity).IsInEnum();
            RuleFor(x => x.SmtpUsername).MaximumLength(320);
        });

        When(x => x.Provider == MailProvider.MicrosoftGraph, () => {
            RuleFor(x => x.GraphTenantId).NotEmpty().MaximumLength(255);
            RuleFor(x => x.GraphClientId).NotEmpty().MaximumLength(255);
            RuleFor(x => x.GraphMailbox).NotEmpty().EmailAddress().MaximumLength(320);
        });
    }
}
