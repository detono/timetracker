using FluentValidation;

namespace TimeTracker.Application.Company.Commands.UpdateCompany;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand> {
    public UpdateCompanyCommandValidator() {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Street).MaximumLength(200);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.CountryCode).Matches("^[A-Za-z]{2}$").When(x => !string.IsNullOrWhiteSpace(x.CountryCode))
            .WithMessage("The country must be a two-letter code, e.g. BE.");
        RuleFor(x => x.Region).MaximumLength(10);
    }
}
