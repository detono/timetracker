using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Dtos;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserDto>>> {
    public async Task<Result<IReadOnlyList<UserDto>>>
        Handle(GetUsersQuery request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<IReadOnlyList<UserDto>>.Failure("Only an employer can list all users.",
                ResultErrorType.Forbidden);
        }

        var users = await unitOfWork.Users.GetAllAsync(cancellationToken);

        var dtos = users
            .Select(u =>
                new UserDto(u.Id, u.FirstName, u.LastName, u.Email, u.Role.ToString(), u.SupervisorId, u.IsActive))
            .OrderBy(u => u.LastName)
            .ToList();

        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }
}