using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.HourTypes.Queries.GetHourTypes;

public class GetHourTypesQueryHandler(
    IUnitOfWork unitOfWork, 
    ICurrentUserService currentUser
) : IRequestHandler<GetHourTypesQuery, Result<IReadOnlyList<HourTypeDto>>> {
    public async Task<Result<IReadOnlyList<HourTypeDto>>> Handle(GetHourTypesQuery request,
        CancellationToken cancellationToken) {
        var includeInactive = request.IncludeInactive && currentUser.Role == UserRole.Employer;

        var types = await unitOfWork.HourTypes.GetAllAsync(includeInactive, cancellationToken);

        // Swapped t.Name for t.LocalizedNames, and added t.IsDefault
        var dtos = types.Select(t => new HourTypeDto(
            t.Id,
            t.LocalizedNames,
            t.ColorHex,
            t.IsActive,
            t.IsDefault
        )).ToList();

        return Result<IReadOnlyList<HourTypeDto>>.Success(dtos);
    }
}