using TimeTracker.Domain.Enums;

namespace TimeTracker.Application.Common.Interfaces;

/// <summary>
/// Exposes the identity of the caller for the duration of the current request,
/// decoupling the Application layer from ASP.NET Core's HttpContext.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    UserRole Role { get; }

    /// <summary>The caller's tenant. Null for platform admins and outside a request (e.g. startup seeding).</summary>
    Guid? TenantId { get; }
    bool IsAuthenticated { get; }

    /// <summary>The caller's name as signed in, e.g. for the activity log.</summary>
    string? FullName => null;
}
