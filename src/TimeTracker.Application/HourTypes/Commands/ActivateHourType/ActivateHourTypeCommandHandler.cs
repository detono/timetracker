using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.HourTypes.Commands.ActivateHourType;

public class ActivateHourTypeCommandHandler : IRequestHandler<ActivateHourTypeCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ActivateHourTypeCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ActivateHourTypeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Employer)
        {
            return Result.Failure("Only an employer can reactivate hour types.", ResultErrorType.Forbidden);
        }

        var hourType = await _unitOfWork.HourTypes.GetByIdAsync(request.Id, cancellationToken);
        if (hourType is null)
        {
            return Result.Failure("Hour type was not found.", ResultErrorType.NotFound);
        }

        hourType.Activate();
        _unitOfWork.HourTypes.Update(hourType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
