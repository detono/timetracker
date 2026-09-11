using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Users.Commands.AssignSupervisor;

public class AssignSupervisorCommandHandler : IRequestHandler<AssignSupervisorCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AssignSupervisorCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AssignSupervisorCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Employer)
        {
            return Result.Failure("Only an employer can assign supervisors.", ResultErrorType.Forbidden);
        }

        var employee = await _unitOfWork.Users.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure("Employee was not found.", ResultErrorType.NotFound);
        }

        if (employee.Role != UserRole.Employee)
        {
            return Result.Failure("Only employees can have a supervisor assigned.", ResultErrorType.Validation);
        }

        if (request.SupervisorId.HasValue)
        {
            var supervisor = await _unitOfWork.Users.GetByIdAsync(request.SupervisorId.Value, cancellationToken);
            if (supervisor is null)
            {
                return Result.Failure("Supervisor was not found.", ResultErrorType.NotFound);
            }

            if (supervisor.Role != UserRole.Employee)
            {
                return Result.Failure(
                    "Only an employee can be assigned as a supervisor (an employer already sees everyone).",
                    ResultErrorType.Validation);
            }
        }

        employee.AssignSupervisor(request.SupervisorId);
        _unitOfWork.Users.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
