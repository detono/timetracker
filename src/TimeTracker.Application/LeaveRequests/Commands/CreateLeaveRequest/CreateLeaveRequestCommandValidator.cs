using FluentValidation;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Application.LeaveRequests.Commands.CreateLeaveRequest;

public class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand> {
    public CreateLeaveRequestCommandValidator() {
        RuleFor(x => x.HourTypeId).NotEmpty();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("The end date can't be before the start date.");
        RuleFor(x => x).Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber < LeaveRequest.MaxRangeDays)
            .WithMessage("A leave request can span at most one year.");
        RuleFor(x => x.Comment).MaximumLength(1000);
        RuleFor(x => x.FirstDayPart).IsInEnum();
        RuleFor(x => x.LastDayPart).IsInEnum();
        RuleFor(x => x.FirstDayPart).NotEqual(DayPart.Morning).When(x => x.EndDate > x.StartDate)
            .WithMessage("Leave over several days can't start with only a morning; choose the whole day or the afternoon.");
        RuleFor(x => x.LastDayPart).NotEqual(DayPart.Afternoon).When(x => x.EndDate > x.StartDate)
            .WithMessage("Leave over several days can't end with only an afternoon; choose the whole day or the morning.");

        When(x => x.FirstDayPart == DayPart.Hours || x.LastDayPart == DayPart.Hours, () => {
            RuleFor(x => x.EndDate).Equal(x => x.StartDate).WithMessage("Leave for a few hours must be on a single day.");
            RuleFor(x => x.StartTime).NotNull().WithMessage("Choose the time your leave starts.");
            RuleFor(x => x.EndTime).NotNull().WithMessage("Choose the time your leave ends.");
            RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).When(x => x.StartTime is not null && x.EndTime is not null)
                .WithMessage("The end time must be after the start time.");
        }).Otherwise(() => {
            RuleFor(x => x.StartTime).Null().WithMessage("Start and end times are only used for leave of a few hours.");
            RuleFor(x => x.EndTime).Null().WithMessage("Start and end times are only used for leave of a few hours.");
        });
    }
}
