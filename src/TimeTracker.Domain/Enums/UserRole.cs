namespace TimeTracker.Domain.Enums;

/// <summary>
/// Defines the supported account types. Employees and Employers always belong to a
/// tenant; a PlatformAdmin belongs to none and only manages tenants and other admins.
/// </summary>
public enum UserRole
{
    Employee = 0,
    Employer = 1,
    PlatformAdmin = 2
}
