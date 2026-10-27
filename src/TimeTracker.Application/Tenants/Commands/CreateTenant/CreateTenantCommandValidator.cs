using FluentValidation;

namespace TimeTracker.Application.Tenants.Commands.CreateTenant;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EmployerFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EmployerLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EmployerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.EmployerPassword).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");
    }
}
