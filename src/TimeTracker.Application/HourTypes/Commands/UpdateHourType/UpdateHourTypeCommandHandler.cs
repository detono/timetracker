using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.HourTypes.Commands.UpdateHourType;

public class UpdateHourTypeCommandHandler : IRequestHandler<UpdateHourTypeCommand, Result<HourTypeDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateHourTypeCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<HourTypeDto>> Handle(UpdateHourTypeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Employer)
        {
            return Result<HourTypeDto>.Failure("Only an employer can edit hour types.", ResultErrorType.Forbidden);
        }

        var hourType = await _unitOfWork.HourTypes.GetByIdAsync(request.Id, cancellationToken);
        if (hourType is null)
        {
            return Result<HourTypeDto>.Failure("Hour type was not found.", ResultErrorType.NotFound);
        }

        if (await _unitOfWork.HourTypes.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            return Result<HourTypeDto>.Failure("An hour type with this name already exists.", ResultErrorType.Conflict);
        }

        hourType.SetName(request.Name);
        hourType.SetColor(request.ColorHex);
        _unitOfWork.HourTypes.Update(hourType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<HourTypeDto>.Success(new HourTypeDto(hourType.Id, hourType.Name, hourType.ColorHex, hourType.IsActive));
    }
}
