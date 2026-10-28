using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Email;

/// <summary>
/// Sends mail as a Microsoft 365 mailbox via <c>POST /users/{mailbox}/sendMail</c>, with an
/// app-only token. Needs the <c>Mail.Send</c> application permission (with admin consent).
/// </summary>
public class GraphMailTransport(
    HttpClient httpClient,
    IGraphTokenProvider tokenProvider,
    ISecretProtector secretProtector
) {
    public const string GraphBaseUrl = "https://graph.microsoft.com/v1.0/";

    private const string PermissionHint =
        "Make sure the app registration has the Mail.Send permission of type Application (not Delegated), " +
        "that an admin granted consent, and, if access is limited to certain mailboxes, that {0} is one of them.";

    public async Task SendAsync(TenantMailSettings settings, EmailMessage message, CancellationToken cancellationToken) {
        if (settings.GraphTenantId is null || settings.GraphClientId is null || settings.GraphMailbox is null
            || settings.GraphClientSecretProtected is null) {
            throw new EmailDeliveryException("Microsoft Graph is not fully configured.");
        }

        var secret = MailDispatcher.UnprotectSecret(secretProtector, settings.GraphClientSecretProtected, "client secret");
        var token = await tokenProvider.GetAccessTokenAsync(settings.GraphTenantId, settings.GraphClientId, secret,
            cancellationToken);

        var mailbox = settings.GraphMailbox;
        using var request = new HttpRequestMessage(HttpMethod.Post,
            new Uri(new Uri(GraphBaseUrl), $"users/{Uri.EscapeDataString(mailbox)}/sendMail"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(BuildPayload(settings, message));

        HttpResponseMessage response;
        try {
            response = await httpClient.SendAsync(request, cancellationToken);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            throw new EmailDeliveryException($"Could not reach Microsoft Graph: {ex.Message}", ex);
        }

        using (response) {
            if (response.IsSuccessStatusCode) {
                return;
            }

            var graphError = await ReadGraphErrorAsync(response, cancellationToken);
            var status = (int)response.StatusCode;
            throw response.StatusCode switch {
                HttpStatusCode.Unauthorized => new EmailDeliveryException(
                    $"Microsoft Graph refused the access token (401{graphError}). " + string.Format(PermissionHint, mailbox)),
                HttpStatusCode.Forbidden => new EmailDeliveryException(
                    $"Microsoft Graph denied sending as {mailbox} (403{graphError}). " + string.Format(PermissionHint, mailbox)),
                HttpStatusCode.NotFound => new EmailDeliveryException(
                    $"Microsoft Graph could not find the mailbox {mailbox} (404{graphError}). Check that it exists and has an Exchange Online licence."),
                _ => new EmailDeliveryException($"Microsoft Graph could not send the email ({status}{graphError}).")
            };
        }
    }

    internal static object BuildPayload(TenantMailSettings settings, EmailMessage message) => new {
        message = new {
            subject = message.Subject,
            body = new { contentType = "HTML", content = message.HtmlBody },
            from = new { emailAddress = new { address = settings.FromAddress, name = settings.FromName ?? settings.FromAddress } },
            toRecipients = message.To.Select(to => new { emailAddress = new { address = to } }).ToArray()
        },
        saveToSentItems = false
    };

    /// <summary>Graph errors look like <c>{"error":{"code":"...","message":"..."}}</c>; returns ": code - message" or "".</summary>
    private static async Task<string> ReadGraphErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
        try {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("error", out var error)) {
                return "";
            }

            var code = error.TryGetProperty("code", out var c) ? c.GetString() : null;
            var text = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            var parts = new[] { code, text }.Where(p => !string.IsNullOrWhiteSpace(p));
            var joined = string.Join(" - ", parts);
            return joined.Length == 0 ? "" : $": {joined}";
        } catch (JsonException) {
            return "";
        }
    }
}
