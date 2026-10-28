using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class WorkScheduleRepository(ApplicationDbContext context) : IWorkScheduleRepository {
    public Task<WorkSchedule?> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.WorkSchedules.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public async Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken = default) =>
        await context.WorkSchedules.AddAsync(schedule, cancellationToken);
}
