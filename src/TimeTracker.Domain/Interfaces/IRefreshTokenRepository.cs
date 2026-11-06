using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Every token handed out for one sign-in on one device.</summary>
    Task<IReadOnlyList<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Deletes the user's expired tokens; kept until then so a replayed one can still be recognised.</summary>
    Task RemoveExpiredForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
