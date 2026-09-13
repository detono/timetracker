using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.HourTypes.Commands.UpdateHourType;

public class UpdateHourTypeCommandHandler(
    IUnitOfWork unitOfWork, 
    ICurrentUserService currentUser
) : IRequestHandler<UpdateHourTypeCommand, Result<HourTypeDto>> {
    public async Task<Result<HourTypeDto>> Handle(UpdateHourTypeCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<HourTypeDto>.Failure("Only an employer can edit hour types.", ResultErrorType.Forbidden);
        }

        var hourType = await unitOfWork.HourTypes.GetByIdAsync(request.Id, cancellationToken);
        if (hourType is null) {
            return Result<HourTypeDto>.Failure("Hour type was not found.", ResultErrorType.NotFound);
        }

        var defaultName = request.LocalizedNames.GetValueOrDefault("en") 
                          ?? request.LocalizedNames.Values.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(defaultName) && 
            await unitOfWork.HourTypes.NameExistsAsync(defaultName, request.Id, cancellationToken)) {
            return Result<HourTypeDto>.Failure("An hour type with this name already exists.", ResultErrorType.Conflict);
        }

        // 1. Clear existing default if this one is claiming the crown
        if (request.IsDefault && !hourType.IsDefault) {
            var allTypes = await unitOfWork.HourTypes.GetAllAsync(true, cancellationToken);
            var existingDefault = allTypes.FirstOrDefault(h => h.IsDefault && h.Id != hourType.Id);
            if (existingDefault != null) {
                existingDefault.Update(existingDefault.LocalizedNames, existingDefault.ColorHex, false);
                unitOfWork.HourTypes.Update(existingDefault);
            }
        }

        // Make sure this Update method accepts request.IsDefault!
        hourType.Update(request.LocalizedNames, request.ColorHex, request.IsDefault);
        
        unitOfWork.HourTypes.Update(hourType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<HourTypeDto>.Success(new HourTypeDto(
            hourType.Id, 
            hourType.LocalizedNames, 
            hourType.ColorHex,
            hourType.IsActive,
            hourType.IsDefault
        ));
    }
}