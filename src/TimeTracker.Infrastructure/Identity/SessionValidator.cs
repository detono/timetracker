using Microsoft.EntityFrameworkCore;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Infrastructure.Identity;

/// <summary>
/// Looks the user and their company up on every request (two primary-key lookups), so a
/// deactivation or password reset signs people out right away instead of when their token expires.
/// </summary>
public class SessionValidator(ApplicationDbContext context) : ISessionValidator {
    public async Task<bool> IsValidAsync(Guid userId, int sessionVersion, CancellationToken cancellationToken = default) {
        // Runs before the caller's tenant is known, so it bypasses the tenant filter on purpose.
        var user = await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.SessionVersion, u.TenantId })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !user.IsActive || user.SessionVersion != sessionVersion) {
            return false;
        }

        return user.TenantId is not { } tenantId
               || await context.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);
    }
}
