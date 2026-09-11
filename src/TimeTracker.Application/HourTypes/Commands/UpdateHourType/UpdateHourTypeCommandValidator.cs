using FluentValidation;

namespace TimeTracker.Application.HourTypes.Commands.UpdateHourType;

public class UpdateHourTypeCommandValidator : AbstractValidator<UpdateHourTypeCommand>
{
    public UpdateHourTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ColorHex).NotEmpty().Matches("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
            .WithMessage("Color must be a hex value like #932e4a.");
    }
}
