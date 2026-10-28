using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface IWorkScheduleRepository {
    /// <summary>The person's own schedule, or null if they follow the company's.</summary>
    Task<WorkSchedule?> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The company's default schedule, or null if none was set.</summary>
    Task<WorkSchedule?> GetCompanyDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>Every saved schedule in the company: personal ones and the company default.</summary>
    Task<IReadOnlyList<WorkSchedule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
    void Remove(WorkSchedule schedule);
}
