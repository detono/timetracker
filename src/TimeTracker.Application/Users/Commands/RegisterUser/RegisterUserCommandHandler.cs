using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Users.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public RegisterUserCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // New accounts always join the creating employer's own tenant.
        if (_currentUser.TenantId is not { } tenantId)
        {
            return Result<UserDto>.Failure("Only users within a tenant can create accounts.", ResultErrorType.Forbidden);
        }

        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
        {
            return Result<UserDto>.Failure("A user with this email already exists.", ResultErrorType.Conflict);
        }

        var hash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.FirstName, request.LastName, request.Email, hash, request.Role, tenantId);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(user.Id, user.FirstName, user.LastName, user.Email, user.Role.ToString(), user.SupervisorId, user.IsActive);
        return Result<UserDto>.Success(dto);
    }
}
