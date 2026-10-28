using FluentValidation;
using TimeTracker.Domain.Entities;

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
    }
}
