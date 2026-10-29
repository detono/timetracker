using System.Security.Claims;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Security;
using TimeTracker.Domain.Enums;

namespace TimeTracker.API.Services;

/// <summary>
/// ASP.NET Core-specific implementation of <see cref="ICurrentUserService"/>, reading from
/// HttpContext. Also the <see cref="ITenantScope"/>, since entering one changes the tenant
/// that the rest of the request sees.
/// </summary>
public class CurrentUserService : ICurrentUserService, ITenantScope
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _scopedTenantId;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public UserRole Role
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(value, out var role) ? role : UserRole.Employee;
        }
    }

    public Guid? TenantId
    {
        get
        {
            if (_scopedTenantId is { } scoped)
            {
                return scoped;
            }

            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(TenantClaimTypes.TenantId);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public IDisposable Enter(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant id is required.", nameof(tenantId));
        }

        // Never switch from one company to another: a company member always stays in their own.
        var current = TenantId;
        if (current is not null && current != tenantId)
        {
            throw new InvalidOperationException("Cannot enter another tenant's scope.");
        }

        var previous = _scopedTenantId;
        _scopedTenantId = tenantId;
        return new Scope(() => _scopedTenantId = previous);
    }

    private sealed class Scope(Action exit) : IDisposable
    {
        private Action? _exit = exit;

        public void Dispose()
        {
            _exit?.Invoke();
            _exit = null;
        }
    }

    public string? FullName => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
