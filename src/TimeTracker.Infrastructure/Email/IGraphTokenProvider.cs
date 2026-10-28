namespace TimeTracker.Infrastructure.Email;

/// <summary>Gets an app-only access token for Microsoft Graph (client-credentials flow).</summary>
public interface IGraphTokenProvider {
    /// <exception cref="TimeTracker.Application.Common.Exceptions.EmailDeliveryException">If Microsoft rejects the credentials.</exception>
    Task<string> GetAccessTokenAsync(string tenantId, string clientId, string clientSecret, CancellationToken cancellationToken);
}
