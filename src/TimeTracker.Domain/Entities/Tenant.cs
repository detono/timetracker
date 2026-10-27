using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A customer company. Every user (except platform admins), hour type, project and time
/// entry belongs to exactly one tenant, and tenants never see each other's data.
/// Deactivating a tenant blocks login for all of its users without deleting anything.
/// </summary>
public class Tenant : BaseEntity {
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private Tenant() {
        // EF Core
    }

    private Tenant(string name) {
        Rename(name);
    }

    public static Tenant Create(string name) => new(name);

    public void Rename(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new DomainException("A tenant must have a name.");
        }

        Name = name.Trim();
        MarkUpdated();
    }

    public void Deactivate() {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate() {
        IsActive = true;
        MarkUpdated();
    }
}
