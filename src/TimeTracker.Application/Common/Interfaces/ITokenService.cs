using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>Issues a signed JWT for the given authenticated user.</summary>
    string GenerateToken(User user);

    /// <summary>
    /// Makes a new refresh token in the given family. <see cref="IssuedRefreshToken.Token"/> goes
    /// to the client once and is never stored; <see cref="IssuedRefreshToken.Entity"/> (with only
    /// its hash) is what the caller saves.
    /// </summary>
    IssuedRefreshToken CreateRefreshToken(User user, Guid familyId);

    /// <summary>The stored form of a refresh token, for looking one up.</summary>
    string HashRefreshToken(string token);
}

public record IssuedRefreshToken(string Token, RefreshToken Entity);
