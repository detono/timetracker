using FluentValidation;

namespace TimeTracker.Application.WorkSchedules.Commands.UpdateWorkSchedule;

public class UpdateWorkScheduleCommandValidator : AbstractValidator<UpdateWorkScheduleCommand> {
    public UpdateWorkScheduleCommandValidator() {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.Days).NotNull();
        RuleFor(x => x.Days).Must(days => days.Select(d => d.DayOfWeek).Distinct().Count() == days.Count)
            .When(x => x.Days is not null)
            .WithMessage("Each weekday can only appear once.");
        RuleForEach(x => x.Days).ChildRules(day => {
            day.RuleFor(d => d.DayOfWeek).IsInEnum();
            day.RuleFor(d => d.EndTime).GreaterThan(d => d.StartTime)
                .WithMessage(d => $"{d.DayOfWeek}: end time must be after start time.");
            day.RuleFor(d => d.BreakMinutes).GreaterThanOrEqualTo(0)
                .Must((d, breakMinutes) => breakMinutes < (d.EndTime - d.StartTime).TotalMinutes)
                .WithMessage(d => $"{d.DayOfWeek}: the break must be shorter than the working day.");
        });
    }
}
