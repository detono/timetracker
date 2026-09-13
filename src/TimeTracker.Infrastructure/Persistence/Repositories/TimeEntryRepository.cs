using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class TimeEntryRepository(ApplicationDbContext context) : ITimeEntryRepository {
    public Task<TimeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.TimeEntries.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TimeEntry>> GetForUserAsync(
        Guid userId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default) {
        var query = context.TimeEntries.AsNoTracking().Where(t => t.UserId == userId);
        query = ApplyDateRange(query, from, to);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeEntry>> GetForUsersAsync(
        IReadOnlyCollection<Guid> userIds, DateOnly? from = null, DateOnly? to = null,
        CancellationToken cancellationToken = default) {
        var query = context.TimeEntries.AsNoTracking().Where(t => userIds.Contains(t.UserId));
        query = ApplyDateRange(query, from, to);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeEntry>> GetAllAsync(
        DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default) {
        var query = context.TimeEntries.AsNoTracking().AsQueryable();
        query = ApplyDateRange(query, from, to);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default) =>
        await context.TimeEntries.AddAsync(timeEntry, cancellationToken);

    public async Task<IReadOnlyList<TimeEntry>>
        GetAllWithProjectIdAsync(CancellationToken cancellationToken = default) {
        return await context.TimeEntries
            .Where(e => e.ProjectId != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeEntry>> GetByProjectIdAsync(Guid projectId,
        CancellationToken cancellationToken = default) {
        return await context.TimeEntries
            .Where(e => e.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public void Update(TimeEntry timeEntry) => context.TimeEntries.Update(timeEntry);

    public void Remove(TimeEntry timeEntry) => context.TimeEntries.Remove(timeEntry);

    private static IQueryable<TimeEntry> ApplyDateRange(IQueryable<TimeEntry> query, DateOnly? from, DateOnly? to) {
        if (from.HasValue) {
            query = query.Where(t => t.WorkDate >= from.Value);
        }

        if (to.HasValue) {
            query = query.Where(t => t.WorkDate <= to.Value);
        }

        return query;
    }
}