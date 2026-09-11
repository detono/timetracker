using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

public class Project : BaseEntity {
    public string Name { get; private set; }
    public string? ClientName { get; private set; }
    public bool IsActive { get; private set; }

    private Project() {
        // EF Core
    }

    private Project(string name, string? clientName) {
        Name = name;
        ClientName = clientName;
        IsActive = true;
    }

    public static Project Create(string name, string? clientName = null) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new DomainException("A project must have a name.");
        }

        return new Project(name, clientName);
    }

    public void Update(string name, string? clientName) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new DomainException("A project must have a name.");
        }

        Name = name;
        ClientName = clientName;
        MarkUpdated();
    }

    public void Activate() {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate() {
        IsActive = false;
        MarkUpdated();
    }
}