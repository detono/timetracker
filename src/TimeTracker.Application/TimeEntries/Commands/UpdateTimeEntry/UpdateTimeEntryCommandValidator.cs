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
    }
}
