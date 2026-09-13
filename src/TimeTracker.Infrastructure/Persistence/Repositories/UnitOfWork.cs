using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class UnitOfWork(
    ApplicationDbContext context,
    IUserRepository users,
    ITimeEntryRepository timeEntries,
    IHourTypeRepository hourTypes,
    IProjectRepository projects
) : IUnitOfWork {
    public IUserRepository Users { get; } = users;
    public ITimeEntryRepository TimeEntries { get; } = timeEntries;
    public IHourTypeRepository HourTypes { get; } = hourTypes;
    public IProjectRepository Projects { get; } = projects;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}