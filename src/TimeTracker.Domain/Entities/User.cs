using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A person that can authenticate into the system. Employees log their own hours;
/// Employers have full visibility. An Employee may optionally be supervised by another
/// Employee (e.g. a team lead) which grants that supervisor read access to their hours,
/// modelling the "necessary authority" requirement without introducing a third role.
/// Every Employee and Employer belongs to a tenant; a PlatformAdmin belongs to none.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }

    /// <summary>The tenant this user belongs to. Null only for a <see cref="UserRole.PlatformAdmin"/>.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// The Id of the Employee who is allowed to view this user's logged hours
    /// in addition to the user themself and any Employer. Null if unsupervised.
    /// </summary>
    public Guid? SupervisorId { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Whether this person's logged hours are compared with their work schedule. Off for
    /// people who don't log their hours, e.g. an owner, so they don't show up as "behind".
    /// </summary>
    public bool TracksHours { get; private set; } = true;

    /// <summary>
    /// The two-letter language the user picked (e.g. "de"), used for the app and for emails.
    /// Null until they pick one; emails then fall back to English.
    /// </summary>
    public string? Language { get; private set; }

    /// <summary>
    /// Goes up whenever existing sign-ins must stop working, e.g. after a password change or
    /// reset. Each sign-in remembers the number it started with and stops working once it's
    /// out of date.
    /// </summary>
    public int SessionVersion { get; private set; }

    /// <summary>
    /// True when someone else set this password (an employer or platform admin). The user
    /// must choose their own before they can do anything else.
    /// </summary>
    public bool MustChangePassword { get; private set; }

    private readonly List<TimeEntry> _timeEntries = new();
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();

    private User()
    {
        // EF Core
    }

    private User(string firstName, string lastName, string email, string passwordHash, UserRole role, Guid? tenantId)
    {
        SetName(firstName, lastName);
        SetEmail(email);
        PasswordHash = passwordHash;
        Role = role;
        TenantId = tenantId;
    }

    /// <summary>Creates an Employee or Employer account inside the given tenant.</summary>
    public static User Create(string firstName, string lastName, string email, string passwordHash, UserRole role, Guid tenantId)
    {
        if (role == UserRole.PlatformAdmin)
        {
            throw new DomainException("Platform admins do not belong to a tenant; use CreatePlatformAdmin.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new DomainException("A user must belong to a tenant.");
        }

        EnsurePasswordHash(passwordHash);
        return new User(firstName, lastName, email, passwordHash, role, tenantId);
    }

    /// <summary>Creates a platform admin, who manages tenants but belongs to none.</summary>
    public static User CreatePlatformAdmin(string firstName, string lastName, string email, string passwordHash)
    {
        EnsurePasswordHash(passwordHash);
        return new User(firstName, lastName, email, passwordHash, UserRole.PlatformAdmin, null);
    }

    private static void EnsurePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("A password hash is required to create a user.");
        }
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

    /// <summary>Sets a password the user chose themself. Signs out their other sessions.</summary>
    public void ChangePassword(string newPasswordHash)
    {
        SetPasswordHash(newPasswordHash);
        MustChangePassword = false;
    }

    /// <summary>
    /// Sets a password someone else chose for this user. Signs them out everywhere, and they
    /// must choose their own password at their next login.
    /// </summary>
    public void SetTemporaryPassword(string newPasswordHash)
    {
        SetPasswordHash(newPasswordHash);
        MustChangePassword = true;
    }

    /// <summary>
    /// For a new account whose first password was picked by whoever created it: the user
    /// chooses their own at their first login.
    /// </summary>
    public void RequirePasswordChange()
    {
        MustChangePassword = true;
        MarkUpdated();
    }

    private void SetPasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("A password hash is required.");
        }

        PasswordHash = newPasswordHash;
        SessionVersion++;
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

    public void SetLanguage(string? language)
    {
        var code = string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();
        if (code is not null && (code.Length != 2 || !code.All(char.IsAsciiLetterLower)))
        {
            throw new DomainException("The language must be a two-letter code, e.g. de.");
        }

        Language = code;
        MarkUpdated();
    }

    public void SetTracksHours(bool tracksHours)
    {
        TracksHours = tracksHours;
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
