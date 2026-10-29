namespace TimeTracker.Application.Common.Interfaces;

/// <summary>
/// Lets a request that has no company of its own (a platform admin, or someone resetting a
/// forgotten password before signing in) work inside one company. Until the returned scope
/// is disposed, data is read and written as that company, with the usual isolation from
/// every other company. Use it only around the few operations that need it.
/// </summary>
public interface ITenantScope
{
    IDisposable Enter(Guid tenantId);
}
