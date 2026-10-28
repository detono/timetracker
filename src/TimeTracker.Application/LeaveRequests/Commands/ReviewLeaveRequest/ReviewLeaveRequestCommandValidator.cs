using FluentValidation;

namespace TimeTracker.Application.LeaveRequests.Commands.ReviewLeaveRequest;

public class ReviewLeaveRequestCommandValidator : AbstractValidator<ReviewLeaveRequestCommand> {
    public ReviewLeaveRequestCommandValidator() {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
