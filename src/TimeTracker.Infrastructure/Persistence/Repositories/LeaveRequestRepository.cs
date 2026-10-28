using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class LeaveRequestRepository(ApplicationDbContext context) : ILeaveRequestRepository {
    public Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LeaveRequest>> GetAsync(
        IReadOnlyCollection<Guid>? userIds,
        LeaveRequestStatus? status = null,
        CancellationToken cancellationToken = default
    ) {
        var query = context.LeaveRequests.AsNoTracking();
        if (userIds is not null) {
            query = query.Where(r => userIds.Contains(r.UserId));
        }

        if (status is not null) {
            query = query.Where(r => r.Status == status);
        }

        return await query.OrderByDescending(r => r.StartDate).ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetActiveOverlappingAsync(
        Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default
    ) =>
        await context.LeaveRequests.AsNoTracking()
            .Where(r => r.UserId == userId
                        && (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Approved)
                        && r.StartDate <= end && start <= r.EndDate)
            .ToListAsync(cancellationToken);

    public Task<int> CountPendingAsync(IReadOnlyCollection<Guid>? userIds, CancellationToken cancellationToken = default) {
        var query = context.LeaveRequests.Where(r => r.Status == LeaveRequestStatus.Pending);
        if (userIds is not null) {
            query = query.Where(r => userIds.Contains(r.UserId));
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveRequest request, CancellationToken cancellationToken = default) =>
        await context.LeaveRequests.AddAsync(request, cancellationToken);
}
