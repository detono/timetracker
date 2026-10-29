using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface ILockedMonthRepository
{
    Task<IReadOnlyList<LockedMonth>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The locked months that overlap the given dates (both inclusive).</summary>
    Task<IReadOnlyList<LockedMonth>> GetBetweenAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    Task<LockedMonth?> GetAsync(int year, int month, CancellationToken cancellationToken = default);
    Task AddAsync(LockedMonth lockedMonth, CancellationToken cancellationToken = default);
    void Remove(LockedMonth lockedMonth);
}
