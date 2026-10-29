using TimeTracker.Application.Common.Security;

namespace TimeTracker.API.Authorization;

public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires a token that belongs to a tenant. Applied to every controller serving
    /// tenant data, so a platform admin (who has no tenant) can't reach those endpoints.
    /// </summary>
    public const string TenantMember = "TenantMember";

    public static void AddTenantPolicies(this Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
    {
        // Endpoints are closed unless they say otherwise: one that forgets [Authorize] still
        // requires a signed-in user. Sign-in, password reset and /health opt out explicitly.
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.AddPolicy(TenantMember, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(TenantClaimTypes.TenantId));
    }
}
