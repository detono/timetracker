using FluentValidation;

namespace TimeTracker.Application.LeaveBalances.Commands.SetLeaveAllowance;

public class SetLeaveAllowanceCommandValidator : AbstractValidator<SetLeaveAllowanceCommand> {
    public SetLeaveAllowanceCommandValidator() {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.AllowanceHours).InclusiveBetween(0, 10_000);
    }
}
