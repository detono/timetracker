using Microsoft.EntityFrameworkCore;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository {
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.Where(t => t.FamilyId == familyId).ToListAsync(cancellationToken);

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.AddAsync(token, cancellationToken);

    public async Task RemoveExpiredForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default) {
        var expired = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.ExpiresAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);
        context.RefreshTokens.RemoveRange(expired);
    }
}
