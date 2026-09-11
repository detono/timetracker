using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.HourTypes.Commands.CreateHourType;

public class CreateHourTypeCommandHandler : IRequestHandler<CreateHourTypeCommand, Result<HourTypeDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateHourTypeCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<HourTypeDto>> Handle(CreateHourTypeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Employer)
        {
            return Result<HourTypeDto>.Failure("Only an employer can create hour types.", ResultErrorType.Forbidden);
        }

        if (await _unitOfWork.HourTypes.NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            return Result<HourTypeDto>.Failure("An hour type with this name already exists.", ResultErrorType.Conflict);
        }

        var hourType = HourType.Create(request.Name, request.ColorHex);
        await _unitOfWork.HourTypes.AddAsync(hourType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<HourTypeDto>.Success(new HourTypeDto(hourType.Id, hourType.Name, hourType.ColorHex, hourType.IsActive));
    }
}
