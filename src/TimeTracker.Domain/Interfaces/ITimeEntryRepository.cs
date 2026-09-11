using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimeEntry>> GetForUserAsync(
        Guid userId,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimeEntry>> GetForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimeEntry>> GetAllAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default);
    void Update(TimeEntry timeEntry);
    void Remove(TimeEntry timeEntry);
}
