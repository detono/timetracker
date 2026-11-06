using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth.Commands.SignOut;

public class SignOutCommandHandler(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IDateTimeProvider clock
) : IRequestHandler<SignOutCommand, Result> {
    public async Task<Result> Handle(SignOutCommand request, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(request.RefreshToken) || request.RefreshToken.Length > 200) {
            return Result.Success();
        }

        var token = await unitOfWork.RefreshTokens.GetByHashAsync(
            tokenService.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (token is null) {
            return Result.Success();
        }

        var now = clock.UtcNow;
        foreach (var member in await unitOfWork.RefreshTokens.GetFamilyAsync(token.FamilyId, cancellationToken)) {
            member.Revoke(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
