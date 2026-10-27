using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Application.UnitTests;

/// <summary>Simple test double so handler tests don't need to mock HttpContext plumbing.</summary>
public class TestCurrentUserService : ICurrentUserService
{
    public TestCurrentUserService(Guid userId, UserRole role, Guid? tenantId = null)
    {
        UserId = userId;
        Role = role;
        TenantId = tenantId;
    }

    public Guid UserId { get; }
    public UserRole Role { get; }
    public Guid? TenantId { get; }
    public bool IsAuthenticated => true;
}
