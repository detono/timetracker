using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

public class Project : TenantOwnedEntity {
    /// <summary>The largest budget a project can have, in hours.</summary>
    public const decimal MaxBudgetHours = 100_000;

    public string Name { get; private set; }
    public string? ClientName { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>How many hours the project may take in total, if it has a budget.</summary>
    public decimal? BudgetHours { get; private set; }

    /// <summary>Whether its hours are charged to the client, so reports can tell them apart.</summary>
    public bool IsBillable { get; private set; }

    private List<ProjectMember> _members = new();

    /// <summary>
    /// The people who may log hours on the project. Empty means everyone in the company may.
    /// </summary>
    public IReadOnlyList<ProjectMember> Members => _members.AsReadOnly();

    public bool IsRestricted => _members.Count > 0;

    private Project() {
        // EF Core
        Name = null!;
    }

    private Project(string name, string? clientName, decimal? budgetHours, bool isBillable) {
        Name = name;
        IsActive = true;
        SetDetails(name, clientName, budgetHours, isBillable);
    }

    public static Project Create(string name, string? clientName = null, decimal? budgetHours = null, bool isBillable = false) {
        return new Project(name, clientName, budgetHours, isBillable);
    }

    public void Update(string name, string? clientName, decimal? budgetHours = null, bool isBillable = false) {
        SetDetails(name, clientName, budgetHours, isBillable);
        MarkUpdated();
    }

    private void SetDetails(string name, string? clientName, decimal? budgetHours, bool isBillable) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new DomainException("A project must have a name.");
        }

        if (budgetHours is <= 0 or > MaxBudgetHours) {
            throw new DomainException($"A project's budget must be more than 0 and at most {MaxBudgetHours:0} hours.");
        }

        Name = name;
        ClientName = clientName;
        BudgetHours = budgetHours;
        IsBillable = isBillable;
    }

    /// <summary>Limits logging hours to these people. No one means everyone may.</summary>
    public void SetMembers(IEnumerable<Guid> userIds) {
        var ids = userIds.Distinct().ToList();
        if (ids.Any(id => id == Guid.Empty)) {
            throw new DomainException("A project member must be a user.");
        }

        // Keep the members that stay, so the database only sees who was added or removed.
        _members.RemoveAll(m => !ids.Contains(m.UserId));
        _members.AddRange(ids.Where(id => _members.All(m => m.UserId != id)).Select(id => new ProjectMember(id)));
        MarkUpdated();
    }

    /// <summary>Whether this person may log hours on the project.</summary>
    public bool IsOpenTo(Guid userId) => _members.Count == 0 || _members.Any(m => m.UserId == userId);

    public void Activate() {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate() {
        IsActive = false;
        MarkUpdated();
    }
}

/// <summary>Someone who may log hours on a project that's limited to certain people.</summary>
public record ProjectMember {
    public Guid UserId { get; private init; }

    private ProjectMember() {
        // EF Core
    }

    public ProjectMember(Guid userId) {
        UserId = userId;
    }
}
