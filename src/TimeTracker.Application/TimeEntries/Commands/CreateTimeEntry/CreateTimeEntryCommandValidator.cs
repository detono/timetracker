using FluentValidation;

namespace TimeTracker.Application.TimeEntries.Commands.CreateTimeEntry;

public class CreateTimeEntryCommandValidator : AbstractValidator<CreateTimeEntryCommand> {
    public CreateTimeEntryCommandValidator() {
        RuleFor(x => x.HourTypeId).NotEmpty();
        RuleFor(x => x.WorkDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.BreakMinutes).GreaterThanOrEqualTo(0);
    }
}