using FluentValidation;

namespace TimeTracker.Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand> {
    public UpdateProjectCommandValidator() {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(100).WithMessage("Project name must not exceed 100 characters.");

        RuleFor(v => v.ClientName)
            .MaximumLength(100).WithMessage("Client name must not exceed 100 characters.")
            .When(v => !string.IsNullOrWhiteSpace(v.ClientName));
    }
}