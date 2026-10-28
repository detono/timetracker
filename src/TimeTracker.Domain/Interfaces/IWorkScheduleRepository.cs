using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IWorkScheduleRepository {
    /// <summary>The saved schedule, or null if the user still has the default one.</summary>
    Task<WorkSchedule?> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
}
