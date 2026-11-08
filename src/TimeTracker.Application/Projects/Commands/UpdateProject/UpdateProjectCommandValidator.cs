using FluentValidation;
using TimeTracker.Domain.Entities;

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

        RuleFor(v => v.BudgetHours)
            .GreaterThan(0).WithMessage("The budget must be more than 0 hours.")
            .LessThanOrEqualTo(Project.MaxBudgetHours)
            .WithMessage($"The budget can be at most {Project.MaxBudgetHours:0} hours.")
            .When(v => v.BudgetHours.HasValue);

        RuleFor(v => v.MemberIds)
            .Must(ids => ids is null || ids.All(id => id != Guid.Empty))
            .WithMessage("Each project member must be a person.");
    }
}