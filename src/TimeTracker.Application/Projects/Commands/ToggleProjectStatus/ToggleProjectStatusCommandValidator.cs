using FluentValidation;

namespace TimeTracker.Application.Projects.Commands.ToggleProjectStatus;

public class ToggleProjectStatusCommandValidator : AbstractValidator<ToggleProjectStatusCommand> {
    public ToggleProjectStatusCommandValidator() {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Project ID is required.");

        // No need to validate a boolean (IsActive), as the compiler guarantees it's true or false!
    }
}