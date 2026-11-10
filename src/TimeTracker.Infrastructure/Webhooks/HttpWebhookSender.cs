using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Infrastructure.Email;

namespace TimeTracker.Infrastructure.Webhooks;

/// <summary>
/// POSTs a delivery's JSON to the webhook's URL, signed with its secret:
/// <c>X-Webhook-Signature: sha256=HEX(HMAC-SHA256(secret, "{X-Webhook-Timestamp}.{body}"))</c>.
/// Redirects aren't followed, and unless allowed, only public addresses are called.
/// </summary>
public class HttpWebhookSender(IHttpClientFactory httpClientFactory, ISecretProtector secretProtector, IDateTimeProvider clock)
    : IWebhookSender {
    public const string ClientName = "webhooks";
    private const int MaxErrorBodyLength = 300;

    public async Task<WebhookCallResult> SendAsync(Webhook webhook, WebhookDelivery delivery, CancellationToken cancellationToken) {
        string secret;
        string? headerValue = null;
        try {
            secret = secretProtector.Unprotect(webhook.SecretProtected);
            if (webhook.HeaderValueProtected is not null) {
                headerValue = secretProtector.Unprotect(webhook.HeaderValueProtected);
            }
        } catch (CryptographicException) {
            return new WebhookCallResult(false, null,
                "The stored secret can no longer be decrypted (the server's encryption keys changed). " +
                "Create a new secret, and enter the extra header value again if there is one.", false);
        }

        var timestamp = new DateTimeOffset(clock.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url) {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Webhook-Id", delivery.EventId.ToString());
        request.Headers.Add("X-Webhook-Delivery", delivery.Id.ToString());
        request.Headers.Add("X-Webhook-Event", delivery.EventType);
        request.Headers.Add("X-Webhook-Timestamp", timestamp);
        request.Headers.Add("X-Webhook-Signature", "sha256=" + Sign(secret, timestamp, delivery.Payload));
        if (webhook.HeaderName is not null && headerValue is not null) {
            request.Headers.TryAddWithoutValidation(webhook.HeaderName, headerValue);
        }

        try {
            using var response = await httpClientFactory.CreateClient(ClientName).SendAsync(request, cancellationToken);
            var status = (int)response.StatusCode;
            if (response.IsSuccessStatusCode) {
                return WebhookCallResult.Success(status);
            }

            var body = await ReadStartAsync(response, cancellationToken);
            var reason = status is >= 300 and < 400
                ? $"The address answered with a redirect ({status}), which isn't followed. Use the final address."
                : $"The address answered {status} {response.ReasonPhrase}{(body.Length > 0 ? $": {body}" : "")}";
            // A 4xx won't fix itself by waiting, except "too many requests" and "timeout".
            var retry = status is >= 500 or 408 or 429;
            return new WebhookCallResult(false, status, reason, retry);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return new WebhookCallResult(false, null, "The address didn't answer in time.", true);
        } catch (HttpRequestException ex) when (FindInner<AddressNotAllowedException>(ex) is { } notAllowed) {
            return new WebhookCallResult(false, null, notAllowed.Message, false);
        } catch (HttpRequestException ex) {
            return new WebhookCallResult(false, null, $"Couldn't reach the address: {Innermost(ex).Message}", true);
        }
    }

    public static string Sign(string secret, string timestamp, string body) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}")))
            .ToLowerInvariant();

    private static async Task<string> ReadStartAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
        try {
            var text = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            return text.Length > MaxErrorBodyLength ? text[..MaxErrorBodyLength] + "…" : text;
        } catch (Exception ex) when (ex is HttpRequestException or IOException) {
            return "";
        }
    }

    private static T? FindInner<T>(Exception ex) where T : Exception {
        for (Exception? e = ex; e is not null; e = e.InnerException) {
            if (e is T match) return match;
        }

        return null;
    }

    private static Exception Innermost(Exception ex) {
        while (ex.InnerException is not null) ex = ex.InnerException;
        return ex;
    }

    /// <summary>
    /// The handler behind the webhook client. It connects to an address it checked itself, so
    /// a name can't resolve to a public address when checked and an internal one when used.
    /// </summary>
    public static SocketsHttpHandler CreateHandler(WebhookOptions options) => new() {
        AllowAutoRedirect = false,
        // A proxy would make the connection check look at the proxy instead of the receiver.
        UseProxy = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectTimeout = TimeSpan.FromSeconds(10),
        ConnectCallback = async (context, cancellationToken) => {
            var host = context.DnsEndPoint.Host;
            var addresses = IPAddress.TryParse(host, out var literal)
                ? new[] { literal }
                : await Dns.GetHostAddressesAsync(host, cancellationToken);
            var allowed = options.AllowInternalHosts
                ? addresses
                : addresses.Where(a => !NetworkAddresses.IsInternal(a)).ToArray();
            if (allowed.Length == 0) {
                throw new AddressNotAllowedException(
                    $"{host} is on an internal network, and webhooks may only call public addresses here. " +
                    "Ask whoever runs this server to allow internal addresses (WEBHOOK_ALLOW_INTERNAL_HOSTS=true).");
            }

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try {
                await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            } catch {
                socket.Dispose();
                throw;
            }
        }
    };

    public sealed class AddressNotAllowedException(string message) : Exception(message);
}
