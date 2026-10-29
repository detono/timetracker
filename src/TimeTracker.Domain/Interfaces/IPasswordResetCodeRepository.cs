using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IPasswordResetCodeRepository
{
    /// <summary>The most recently created code for the user, used or not.</summary>
    Task<PasswordResetCode?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordResetCode code, CancellationToken cancellationToken = default);
    Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
