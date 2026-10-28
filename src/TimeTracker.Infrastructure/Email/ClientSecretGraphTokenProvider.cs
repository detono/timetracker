using System.Text.RegularExpressions;
using Azure.Core;
using Azure.Identity;
using TimeTracker.Application.Common.Exceptions;

namespace TimeTracker.Infrastructure.Email;

public partial class ClientSecretGraphTokenProvider : IGraphTokenProvider {
    private static readonly string[] Scopes = { "https://graph.microsoft.com/.default" };

    public async Task<string> GetAccessTokenAsync(string tenantId, string clientId, string clientSecret,
        CancellationToken cancellationToken) {
        try {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var token = await credential.GetTokenAsync(new TokenRequestContext(Scopes), cancellationToken);
            return token.Token;
        } catch (ArgumentException ex) {
            throw new EmailDeliveryException($"The Microsoft tenant id or client id is not valid: {ex.Message}", ex);
        } catch (AuthenticationFailedException ex) {
            // Keep only Microsoft's "AADSTSxxxx: reason" part; the full message is a wall of links.
            var reason = AadErrorPattern().Match(ex.Message) is { Success: true } match ? $" ({match.Value})" : "";
            throw new EmailDeliveryException(
                "Microsoft rejected the app credentials. Check the tenant id, client id and client secret " +
                $"(client secrets expire){reason}.", ex);
        }
    }

    [GeneratedRegex(@"AADSTS\d+:[^\r\n.]*")]
    private static partial Regex AadErrorPattern();
}
