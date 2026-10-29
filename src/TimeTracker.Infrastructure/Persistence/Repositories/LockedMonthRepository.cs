using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class LockedMonthRepository(ApplicationDbContext context) : ILockedMonthRepository {
    public async Task<IReadOnlyList<LockedMonth>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.LockedMonths.AsNoTracking()
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LockedMonth>> GetBetweenAsync(
        DateOnly start, DateOnly end, CancellationToken cancellationToken = default
    ) {
        var from = LockedMonth.Index(start);
        var to = LockedMonth.Index(end);
        return await context.LockedMonths.AsNoTracking()
            .Where(m => m.Year * 12 + m.Month >= from && m.Year * 12 + m.Month <= to)
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync(cancellationToken);
    }

    public Task<LockedMonth?> GetAsync(int year, int month, CancellationToken cancellationToken = default) =>
        context.LockedMonths.FirstOrDefaultAsync(m => m.Year == year && m.Month == month, cancellationToken);

    public async Task AddAsync(LockedMonth lockedMonth, CancellationToken cancellationToken = default) =>
        await context.LockedMonths.AddAsync(lockedMonth, cancellationToken);

    public void Remove(LockedMonth lockedMonth) => context.LockedMonths.Remove(lockedMonth);
}
