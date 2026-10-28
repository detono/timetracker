using TimeTracker.Domain.Entities;

namespace TimeTracker.Domain.Interfaces;

public interface ILeaveBalanceRepository {
    Task<LeaveBalance?> GetAsync(Guid userId, int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveBalance>> GetForYearAsync(int year, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveBalance balance, CancellationToken cancellationToken = default);
}
