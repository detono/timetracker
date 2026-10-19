using FluentValidation;

namespace TimeTracker.Application.TimeEntries.Commands.UpdateTimeEntry;

public class UpdateTimeEntryCommandValidator : AbstractValidator<UpdateTimeEntryCommand>
{
    public UpdateTimeEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.HourTypeId).NotEmpty();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
        RuleFor(x => x.BreakMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Location).MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Longitude).NotNull().When(x => x.Latitude.HasValue)
            .WithMessage("Latitude and longitude must be provided together.");
        RuleFor(x => x.Latitude).NotNull().When(x => x.Longitude.HasValue)
            .WithMessage("Latitude and longitude must be provided together.");
    }
}
