using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class PasswordResetCodeRepository(ApplicationDbContext context) : IPasswordResetCodeRepository {
    public Task<PasswordResetCode?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.PasswordResetCodes
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(PasswordResetCode code, CancellationToken cancellationToken = default) =>
        await context.PasswordResetCodes.AddAsync(code, cancellationToken);

    public async Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken = default) {
        var codes = await context.PasswordResetCodes.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
        context.PasswordResetCodes.RemoveRange(codes);
    }
}
