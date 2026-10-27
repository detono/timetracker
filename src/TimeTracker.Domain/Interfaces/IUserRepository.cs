using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Looks a user up by email across all tenants (emails are globally unique), e.g. for login.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetSupervisedByAsync(Guid supervisorId, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    /// <summary>Checks across all tenants, since emails are globally unique.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
