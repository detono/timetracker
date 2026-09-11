using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.HourTypes.Queries.GetHourTypes;

public class GetHourTypesQueryHandler : IRequestHandler<GetHourTypesQuery, Result<IReadOnlyList<HourTypeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetHourTypesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<HourTypeDto>>> Handle(GetHourTypesQuery request, CancellationToken cancellationToken)
    {
        var includeInactive = request.IncludeInactive && _currentUser.Role == UserRole.Employer;

        var types = await _unitOfWork.HourTypes.GetAllAsync(includeInactive, cancellationToken);

        var dtos = types.Select(t => new HourTypeDto(t.Id, t.Name, t.ColorHex, t.IsActive)).ToList();

        return Result<IReadOnlyList<HourTypeDto>>.Success(dtos);
    }
}
