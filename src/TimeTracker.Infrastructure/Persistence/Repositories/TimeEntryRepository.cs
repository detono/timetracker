using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly ApplicationDbContext _context;

    public TimeEntryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<TimeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.TimeEntries.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TimeEntry>> GetForUserAsync(
        Guid userId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TimeEntries.AsNoTracking().Where(t => t.UserId == userId);
        query = ApplyDateRange(query, from, to);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeEntry>> GetForUsersAsync(
        IReadOnlyCollection<Guid> userIds, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TimeEntries.AsNoTracking().Where(t => userIds.Contains(t.UserId));
        query = ApplyDateRange(query, from, to);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeEntry>> GetAllAsync(
        DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TimeEntries.AsNoTracking().AsQueryable();
        query = ApplyDateRange(query, from, to);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default) =>
        await _context.TimeEntries.AddAsync(timeEntry, cancellationToken);

    public void Update(TimeEntry timeEntry) => _context.TimeEntries.Update(timeEntry);

    public void Remove(TimeEntry timeEntry) => _context.TimeEntries.Remove(timeEntry);

    private static IQueryable<TimeEntry> ApplyDateRange(IQueryable<TimeEntry> query, DateOnly? from, DateOnly? to)
    {
        if (from.HasValue)
        {
            query = query.Where(t => t.WorkDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.WorkDate <= to.Value);
        }

        return query;
    }
}
