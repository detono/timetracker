using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A person that can authenticate into the system. Employees log their own hours;
/// Employers have full visibility. An Employee may optionally be supervised by another
/// Employee (e.g. a team lead) which grants that supervisor read access to their hours,
/// modelling the "necessary authority" requirement without introducing a third role.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }

    /// <summary>
    /// The Id of the Employee who is allowed to view this user's logged hours
    /// in addition to the user themself and any Employer. Null if unsupervised.
    /// </summary>
    public Guid? SupervisorId { get; private set; }

    public bool IsActive { get; private set; } = true;

    private readonly List<TimeEntry> _timeEntries = new();
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();

    private User()
    {
        // EF Core
    }

    private User(string firstName, string lastName, string email, string passwordHash, UserRole role)
    {
        SetName(firstName, lastName);
        SetEmail(email);
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User Create(string firstName, string lastName, string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("A password hash is required to create a user.");
        }

        return new User(firstName, lastName, email, passwordHash, role);
    }

    public void SetName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Last name is required.");
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MarkUpdated();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainException("A valid email address is required.");
        }

        Email = email.Trim().ToLowerInvariant();
        MarkUpdated();
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("A password hash is required.");
        }

        PasswordHash = newPasswordHash;
        MarkUpdated();
    }

    public void AssignSupervisor(Guid? supervisorId)
    {
        if (supervisorId == Id)
        {
            throw new DomainException("A user cannot supervise themselves.");
        }

        SupervisorId = supervisorId;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    /// <summary>
    /// Determines whether this user is authorised to view the hours logged by <paramref name="targetUserId"/>.
    /// </summary>
    public bool CanViewHoursOf(Guid targetUserId, Guid? targetSupervisorId)
    {
        if (Role == UserRole.Employer)
        {
            return true;
        }

        if (Id == targetUserId)
        {
            return true;
        }

        return targetSupervisorId.HasValue && targetSupervisorId.Value == Id;
    }
}
