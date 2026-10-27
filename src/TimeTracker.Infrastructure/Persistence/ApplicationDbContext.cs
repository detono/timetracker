using Microsoft.EntityFrameworkCore;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Persistence;

/// <summary>
/// Tenant isolation lives here so no handler or repository has to remember it:
/// every query on tenant data is filtered to the caller's tenant, and every insert is
/// stamped with it. Bypassing that requires an explicit <c>IgnoreQueryFilters()</c>,
/// which is reserved for login, global email checks and platform-admin features.
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService currentUser
) : DbContext(options) {
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<HourType> HourTypes => Set<HourType>();
    public DbSet<Project> Projects => Set<Project>();

    // Read per query by the filters below; EF re-evaluates it for every query on this instance.
    private Guid? CurrentTenantId => currentUser.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ConfigureTenantOwned<HourType>(modelBuilder);
        ConfigureTenantOwned<Project>(modelBuilder);
        ConfigureTenantOwned<TimeEntry>(modelBuilder);

        // Platform admins (TenantId null) only match each other here, never tenant users.
        modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == CurrentTenantId);

        base.OnModelCreating(modelBuilder);
    }

    private void ConfigureTenantOwned<T>(ModelBuilder modelBuilder) where T : TenantOwnedEntity {
        var builder = modelBuilder.Entity<T>();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.TenantId);
        builder.HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) {
        ApplyTenantRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
        ApplyTenantRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Stamps new tenant data with the caller's tenant and refuses any write that would
    /// touch another tenant's data. Throws instead of silently skipping, since reaching
    /// this means a bug that would otherwise leak or corrupt data across tenants.
    /// </summary>
    private void ApplyTenantRules() {
        var tenantId = CurrentTenantId;

        foreach (var entry in ChangeTracker.Entries<TenantOwnedEntity>()) {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty) {
                if (tenantId is null) {
                    throw new InvalidOperationException(
                        $"Cannot create a {entry.Metadata.ClrType.Name} without a tenant.");
                }

                entry.Entity.AssignToTenant(tenantId.Value);
            }

            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) {
                EnsureSameTenant(entry.Entity.TenantId, tenantId, entry.State, entry.Metadata.ClrType.Name);
            }
        }

        foreach (var entry in ChangeTracker.Entries<User>()) {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) {
                continue;
            }

            if (entry.Entity.Role == UserRole.PlatformAdmin) {
                // Only platform admins (or startup seeding, which has no tenant either) manage admins.
                if (tenantId is not null) {
                    throw new InvalidOperationException("Tenant users cannot modify platform admin accounts.");
                }

                continue;
            }

            EnsureSameTenant(entry.Entity.TenantId, tenantId, entry.State, nameof(User));
        }
    }

    private static void EnsureSameTenant(Guid? recordTenantId, Guid? callerTenantId, EntityState state, string type) {
        if (callerTenantId is null) {
            // A platform admin may create data for a tenant (provisioning), but never edit or delete it.
            if (state != EntityState.Added) {
                throw new InvalidOperationException($"Cannot {state.ToString().ToLower()} a {type} without a tenant.");
            }

            return;
        }

        if (recordTenantId != callerTenantId) {
            throw new InvalidOperationException($"Cannot write a {type} that belongs to another tenant.");
        }
    }
}
