using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.HourTypes.Commands.CreateHourType;

public class CreateHourTypeCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<CreateHourTypeCommand, Result<HourTypeDto>> {
    public async Task<Result<HourTypeDto>> Handle(CreateHourTypeCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<HourTypeDto>.Failure("Only an employer can create hour types.", ResultErrorType.Forbidden);
        }

        // Check every provided locale's value, not just English - a collision in any single
        // language should still be rejected, even if the others are unique.
        foreach (var localizedName in request.LocalizedNames.Values) {
            if (string.IsNullOrWhiteSpace(localizedName)) {
                continue;
            }

            if (await unitOfWork.HourTypes.NameExistsAsync(localizedName, cancellationToken: cancellationToken)) {
                return Result<HourTypeDto>.Failure("An hour type with this name already exists.",
                    ResultErrorType.Conflict);
            }
        }

        // 1. Clear existing default if this new one is meant to be the default
        if (request.IsDefault) {
            var allTypes = await unitOfWork.HourTypes.GetAllAsync(true, cancellationToken);
            var existingDefault = allTypes.FirstOrDefault(h => h.IsDefault);
            if (existingDefault != null) {
                existingDefault.Update(existingDefault.LocalizedNames, existingDefault.ColorHex, false);
                unitOfWork.HourTypes.Update(existingDefault);
            }
        }

        // Make sure your HourType.Create method in the domain accepts request.IsDefault!
        var hourType = HourType.Create(request.LocalizedNames, request.ColorHex, request.IsDefault);

        await unitOfWork.HourTypes.AddAsync(hourType, cancellationToken);
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