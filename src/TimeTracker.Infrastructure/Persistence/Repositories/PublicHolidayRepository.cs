using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class PublicHolidayRepository(ApplicationDbContext context) : IPublicHolidayRepository {
    public Task<PublicHoliday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.PublicHolidays.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PublicHoliday>> GetBetweenAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default
    ) =>
        await context.PublicHolidays.AsNoTracking()
            .Where(h => h.Date >= from && h.Date <= to)
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PublicHoliday holiday, CancellationToken cancellationToken = default) =>
        await context.PublicHolidays.AddAsync(holiday, cancellationToken);

    public void Remove(PublicHoliday holiday) => context.PublicHolidays.Remove(holiday);
}
