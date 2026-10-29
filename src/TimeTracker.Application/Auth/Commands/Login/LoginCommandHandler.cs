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
    private readonly ILoginThrottle _throttle;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenService tokenService, ILoginThrottle throttle)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _throttle = throttle;
    }

    public static string LockedOutMessage(TimeSpan remaining) =>
        $"Too many failed sign-in attempts. Try again in {Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} minutes, " +
        "or reset your password.";

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Checked before the password, so even the right password doesn't get through while blocked.
        if (_throttle.RemainingLockout(request.Email) is { } remaining)
        {
            return Result<AuthResultDto>.Failure(LockedOutMessage(remaining), ResultErrorType.TooManyRequests);
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        // Constant-shape failure message to avoid leaking which part (email/password) was wrong.
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _throttle.RecordFailure(request.Email);
            return Result<AuthResultDto>.Failure("Invalid email or password.", ResultErrorType.Validation);
        }

        _throttle.Reset(request.Email);

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

        return Result<AuthResultDto>.Success(AuthResults.Create(user, tenantName, _tokenService));
    }
}
