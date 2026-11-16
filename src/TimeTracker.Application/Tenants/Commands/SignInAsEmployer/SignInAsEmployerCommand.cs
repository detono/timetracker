using MediatR;
using TimeTracker.Application.AuditLog;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Tenants.Commands.SignInAsEmployer;

/// <summary>
/// Platform admin only: a short sign-in as one of a company's employers, to help them set
/// things up (webhooks, email, …). It's recorded in the company's activity log, as is every
/// change made with it. No refresh token; the admin's own sign-in is untouched.
/// </summary>
public record SignInAsEmployerCommand(Guid TenantId, Guid UserId) : IRequest<Result<AuthResultDto>>;

public class SignInAsEmployerCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ITenantScope tenantScope,
    ITokenService tokenService,
    AuditTrail audit
) : IRequestHandler<SignInAsEmployerCommand, Result<AuthResultDto>> {
    public async Task<Result<AuthResultDto>> Handle(SignInAsEmployerCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.PlatformAdmin || currentUser.ImpersonatorId is not null) {
            return Result<AuthResultDto>.Failure(
                "Only a platform admin can sign in as an employer.", ResultErrorType.Forbidden);
        }

        var tenant = await unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null) {
            return Result<AuthResultDto>.Failure("Company was not found.", ResultErrorType.NotFound);
        }

        if (!tenant.IsActive) {
            return Result<AuthResultDto>.Failure("This company is deactivated. Reactivate it first.");
        }

        using var scope = tenantScope.Enter(request.TenantId);

        // Only finds users of this company, thanks to the scope.
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is not { Role: UserRole.Employer }) {
            return Result<AuthResultDto>.Failure("Employer was not found in this company.", ResultErrorType.NotFound);
        }

        if (!user.IsActive) {
            return Result<AuthResultDto>.Failure("This employer is deactivated.");
        }

        var adminName = string.IsNullOrWhiteSpace(currentUser.FullName) ? "Platform admin" : currentUser.FullName;
        var token = tokenService.GenerateImpersonationToken(user, currentUser.UserId, adminName);

        await audit.RecordAsync(AuditAction.SupportSignIn, user, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResultDto>.Success(new AuthResultDto(
            token, user.Id, $"{user.FirstName} {user.LastName}", user.Role.ToString(), tenant.Name, user.Language));
    }
}
