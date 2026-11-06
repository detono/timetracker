using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.Auth;

internal static class AuthResults
{
    public static AuthResultDto Create(User user, string? tenantName, ITokenService tokenService) =>
        new(tokenService.GenerateToken(user), user.Id, $"{user.FirstName} {user.LastName}", user.Role.ToString(),
            tenantName, user.Language, user.MustChangePassword);

    /// <summary>
    /// Adds a new refresh token in <paramref name="familyId"/> (saved with the caller's next
    /// SaveChanges) and clears the user's expired ones. Returns the token for the client.
    /// </summary>
    public static async Task<string> AddRefreshTokenAsync(
        IUnitOfWork unitOfWork, ITokenService tokenService, User user, Guid familyId, CancellationToken cancellationToken)
    {
        var issued = tokenService.CreateRefreshToken(user, familyId);
        await unitOfWork.RefreshTokens.RemoveExpiredForUserAsync(user.Id, issued.Entity.CreatedAtUtc, cancellationToken);
        await unitOfWork.RefreshTokens.AddAsync(issued.Entity, cancellationToken);
        return issued.Token;
    }
}
