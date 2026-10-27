namespace TimeTracker.Application.Common.Security;

public static class TenantClaimTypes
{
    /// <summary>JWT claim carrying the signed-in user's tenant id. Absent for platform admins.</summary>
    public const string TenantId = "tenant_id";
}
