using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class WorkScheduleRepository(ApplicationDbContext context) : IWorkScheduleRepository {
    public Task<WorkSchedule?> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.WorkSchedules.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public Task<WorkSchedule?> GetCompanyDefaultAsync(CancellationToken cancellationToken = default) =>
        context.WorkSchedules.FirstOrDefaultAsync(s => s.UserId == null, cancellationToken);

    public async Task<IReadOnlyList<WorkSchedule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.WorkSchedules.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken = default) =>
        await context.WorkSchedules.AddAsync(schedule, cancellationToken);

    public void Remove(WorkSchedule schedule) => context.WorkSchedules.Remove(schedule);
}
