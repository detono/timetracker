using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context, IUserRepository users, ITimeEntryRepository timeEntries)
    {
        _context = context;
        Users = users;
        TimeEntries = timeEntries;
    }

    public IUserRepository Users { get; }
    public ITimeEntryRepository TimeEntries { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
