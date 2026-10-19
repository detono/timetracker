using FluentValidation;

namespace TimeTracker.Application.TimeEntries.Commands.CreateTimeEntry;

public class CreateTimeEntryCommandValidator : AbstractValidator<CreateTimeEntryCommand> {
    public CreateTimeEntryCommandValidator() {
        RuleFor(x => x.HourTypeId).NotEmpty();
        RuleFor(x => x.WorkDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
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