using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Domain.Interfaces;

public interface ILeaveRequestRepository {
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Requests of the given users (all users when null), newest first.</summary>
    Task<IReadOnlyList<LeaveRequest>> GetAsync(
        IReadOnlyCollection<Guid>? userIds,
        LeaveRequestStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>Pending or approved requests of the user that overlap the dates.</summary>
    Task<IReadOnlyList<LeaveRequest>> GetActiveOverlappingAsync(
        Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    /// <summary>Pending requests of the given users (all users when null).</summary>
    Task<int> CountPendingAsync(IReadOnlyCollection<Guid>? userIds, CancellationToken cancellationToken = default);

    Task AddAsync(LeaveRequest request, CancellationToken cancellationToken = default);
}
