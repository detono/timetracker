using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// Base for data that belongs to a single <see cref="Tenant"/>. The persistence layer
/// assigns the caller's tenant automatically on insert; <see cref="AssignToTenant"/> is
/// only needed when creating data for a tenant other than the caller's own (e.g. a
/// platform admin seeding default hour types for a new tenant).
/// </summary>
public abstract class TenantOwnedEntity : BaseEntity {
    public Guid TenantId { get; private set; }

    public void AssignToTenant(Guid tenantId) {
        if (tenantId == Guid.Empty) {
            throw new DomainException("A tenant id is required.");
        }

        if (TenantId != Guid.Empty && TenantId != tenantId) {
            throw new DomainException("This record already belongs to another tenant.");
        }

        TenantId = tenantId;
    }
}
