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
    }
}
