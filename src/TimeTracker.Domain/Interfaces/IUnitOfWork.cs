namespace TimeTracker.Domain.Interfaces;

/// <summary>
/// Coordinates the persistence of changes across repositories within a single transaction/context.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ITimeEntryRepository TimeEntries { get; }
    IHourTypeRepository HourTypes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
