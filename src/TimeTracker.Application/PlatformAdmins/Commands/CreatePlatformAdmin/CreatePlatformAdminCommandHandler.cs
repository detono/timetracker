using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.PlatformAdmins.Commands.CreatePlatformAdmin;

public class CreatePlatformAdminCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher
) : IRequestHandler<CreatePlatformAdminCommand, Result<UserDto>> {
    public async Task<Result<UserDto>> Handle(CreatePlatformAdminCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.PlatformAdmin) {
            return Result<UserDto>.Failure("Only a platform admin can create platform admins.",
                ResultErrorType.Forbidden);
        }

        if (await unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken)) {
            return Result<UserDto>.Failure("A user with this email already exists.", ResultErrorType.Conflict);
        }

        var admin = User.CreatePlatformAdmin(
            request.FirstName, request.LastName, request.Email, passwordHasher.Hash(request.Password));

        await unitOfWork.Users.AddAsync(admin, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(
            new UserDto(admin.Id, admin.FirstName, admin.LastName, admin.Email, admin.Role.ToString(), null, admin.IsActive));
    }
}
