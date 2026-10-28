using FluentValidation;

namespace TimeTracker.Application.Auth.Commands.SetLanguage;

public class SetLanguageCommandValidator : AbstractValidator<SetLanguageCommand> {
    public SetLanguageCommandValidator() {
        RuleFor(x => x.Language).Matches("^[A-Za-z]{2}$").When(x => !string.IsNullOrWhiteSpace(x.Language))
            .WithMessage("The language must be a two-letter code, e.g. de.");
    }
}
