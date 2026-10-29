using FluentValidation;

namespace TimeTracker.Application.Auth.Commands.ResetForgottenPassword;

public class ResetForgottenPasswordCommandValidator : AbstractValidator<ResetForgottenPasswordCommand> {
    public ResetForgottenPasswordCommandValidator() {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage("New password must be at least 8 characters long.");
    }
}
