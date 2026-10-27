using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.PlatformAdmins.Queries.GetPlatformAdmins;

public class GetPlatformAdminsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<GetPlatformAdminsQuery, Result<IReadOnlyList<UserDto>>> {
    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetPlatformAdminsQuery request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.PlatformAdmin) {
            return Result<IReadOnlyList<UserDto>>.Failure("Only a platform admin can list platform admins.",
                ResultErrorType.Forbidden);
        }

        // A platform admin has no tenant, so the tenant filter already limits this to platform admins.
        var users = await unitOfWork.Users.GetAllAsync(cancellationToken);

        var dtos = users
            .Where(u => u.Role == UserRole.PlatformAdmin)
            .Select(u => new UserDto(u.Id, u.FirstName, u.LastName, u.Email, u.Role.ToString(), null, u.IsActive))
            .OrderBy(u => u.LastName)
            .ToList();

        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }
}
