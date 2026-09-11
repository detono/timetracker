using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(
        ApplicationDbContext context,
        IUserRepository users,
        ITimeEntryRepository timeEntries,
        IHourTypeRepository hourTypes)
    {
        _context = context;
        Users = users;
        TimeEntries = timeEntries;
        HourTypes = hourTypes;
    }

    public IUserRepository Users { get; }
    public ITimeEntryRepository TimeEntries { get; }
    public IHourTypeRepository HourTypes { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
