using MediatR;
using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth.Commands.RefreshSession;

public class RefreshSessionCommandHandler(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    ITenantScope tenantScope,
    IDateTimeProvider clock
) : IRequestHandler<RefreshSessionCommand, Result<AuthResultDto>> {
    // The same message for every failure: the client just signs in again.
    public const string InvalidMessage = "This sign-in is no longer valid. Please sign in again.";

    public async Task<Result<AuthResultDto>> Handle(RefreshSessionCommand request, CancellationToken cancellationToken) {
        var now = clock.UtcNow;
        var token = await unitOfWork.RefreshTokens.GetByHashAsync(
            tokenService.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (token is null) {
            return Invalid();
        }

        // A used token coming back means a copy exists somewhere: end that whole sign-in.
        if (token.IsReplay(now)) {
            await RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
            return Invalid();
        }

        if (!token.CanBeExchanged(now)) {
            return Invalid();
        }

        using var scope = token.TenantId is { } tenantId ? tenantScope.Enter(tenantId) : null;

        var user = await unitOfWork.Users.GetByIdAsync(token.UserId, cancellationToken);
        Tenant? tenant = null;
        if (user is { TenantId: { } userTenantId }) {
            tenant = await unitOfWork.Tenants.GetByIdAsync(userTenantId, cancellationToken);
        }

        // Deactivated, password changed or reset, or the company was switched off.
        if (user is not { IsActive: true }
            || user.SessionVersion != token.SessionVersion
            || user.TenantId != token.TenantId
            || (user.TenantId is not null && tenant is not { IsActive: true })) {
            await RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
            return Invalid();
        }

        token.MarkUsed(now);
        var refreshToken = await AuthResults.AddRefreshTokenAsync(
            unitOfWork, tokenService, user, token.FamilyId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResultDto>.Success(
            AuthResults.Create(user, tenant?.Name, tokenService) with { RefreshToken = refreshToken });
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken) {
        foreach (var member in await unitOfWork.RefreshTokens.GetFamilyAsync(familyId, cancellationToken)) {
            member.Revoke(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Result<AuthResultDto> Invalid() =>
        Result<AuthResultDto>.Failure(InvalidMessage, ResultErrorType.Validation);
}
