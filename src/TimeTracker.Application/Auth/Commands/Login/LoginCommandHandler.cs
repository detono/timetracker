using MediatR;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        // Constant-shape failure message to avoid leaking which part (email/password) was wrong.
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResultDto>.Failure("Invalid email or password.", ResultErrorType.Validation);
        }

        string? tenantName = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantId.Value, cancellationToken);
            if (tenant is null || !tenant.IsActive)
            {
                return Result<AuthResultDto>.Failure(
                    "Your organisation's account has been deactivated. Please contact your administrator.",
                    ResultErrorType.Forbidden);
            }

            tenantName = tenant.Name;
        }

        var token = _tokenService.GenerateToken(user);
        var dto = new AuthResultDto(token, user.Id, $"{user.FirstName} {user.LastName}", user.Role.ToString(), tenantName, user.Language);

        return Result<AuthResultDto>.Success(dto);
    }
}
