using TimeTracker.Application.Auth.Dtos;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Auth;

internal static class AuthResults
{
    public static AuthResultDto Create(User user, string? tenantName, ITokenService tokenService) =>
        new(tokenService.GenerateToken(user), user.Id, $"{user.FirstName} {user.LastName}", user.Role.ToString(),
            tenantName, user.Language, user.MustChangePassword);
}
