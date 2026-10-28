using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class LeaveBalanceRepository(ApplicationDbContext context) : ILeaveBalanceRepository {
    public Task<LeaveBalance?> GetAsync(Guid userId, int year, CancellationToken cancellationToken = default) =>
        context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == userId && b.Year == year, cancellationToken);

    public async Task<IReadOnlyList<LeaveBalance>> GetForYearAsync(int year, CancellationToken cancellationToken = default) =>
        await context.LeaveBalances.AsNoTracking().Where(b => b.Year == year).ToListAsync(cancellationToken);

    public async Task AddAsync(LeaveBalance balance, CancellationToken cancellationToken = default) =>
        await context.LeaveBalances.AddAsync(balance, cancellationToken);
}
