using System.Net;
using System.Net.Sockets;
using MailKit;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Email;

/// <summary>Sends mail through the tenant's SMTP server with MailKit.</summary>
public class SmtpMailTransport(ISecretProtector secretProtector, IOptions<MailNetworkOptions>? networkOptions = null) {
    private const int TimeoutMilliseconds = 30_000;
    private readonly bool _allowInternalHosts = networkOptions?.Value.AllowInternalHosts ?? false;

    public async Task SendAsync(TenantMailSettings settings, EmailMessage message, CancellationToken cancellationToken) {
        var host = settings.SmtpHost ?? throw new EmailDeliveryException("No SMTP host is configured.");
        var port = settings.SmtpPort;
        var mime = BuildMessage(settings, message);

        using var client = new SmtpClient { Timeout = TimeoutMilliseconds };
        try {
            // Connect to a checked IP address ourselves, so the host can't resolve to something else in between.
            var socket = await ConnectSocketAsync(host, port, cancellationToken);
            await client.ConnectAsync(socket, host, port, ToSocketOptions(settings.SmtpSecurity), cancellationToken);

            if (!string.IsNullOrEmpty(settings.SmtpUsername)) {
                var password = settings.SmtpPasswordProtected is null
                    ? ""
                    : MailDispatcher.UnprotectSecret(secretProtector, settings.SmtpPasswordProtected, "SMTP password");
                await client.AuthenticateAsync(settings.SmtpUsername, password, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (EmailDeliveryException) {
            throw;
        } catch (AuthenticationException ex) {
            throw new EmailDeliveryException(
                "The SMTP server rejected the username or password. Microsoft 365 usually blocks password-based SMTP; " +
                "use Microsoft Graph for Microsoft 365 mailboxes.", ex);
        } catch (SslHandshakeException ex) {
            throw new EmailDeliveryException(
                $"Could not set up a secure connection with {host}:{port}. Check the security setting " +
                "(port 587 normally uses STARTTLS, port 465 uses SSL/TLS).", ex);
        } catch (SocketException ex) {
            throw new EmailDeliveryException(
                $"Could not connect to {host}:{port} ({ex.Message}). Check the host name and port, and that the " +
                "server accepts connections from this server.", ex);
        } catch (Exception ex) when (ex is TimeoutException or OperationCanceledException) {
            throw new EmailDeliveryException($"The SMTP server {host}:{port} did not respond in time.", ex);
        } catch (SmtpCommandException ex) {
            throw new EmailDeliveryException(
                $"The SMTP server refused the message ({(int)ex.StatusCode}): {ex.Message}", ex);
        } catch (SmtpProtocolException ex) {
            throw new EmailDeliveryException($"The SMTP server responded unexpectedly: {ex.Message}", ex);
        } catch (Exception ex) when (ex is NotSupportedException or ServiceNotConnectedException or IOException) {
            throw new EmailDeliveryException($"Sending through {host}:{port} failed: {ex.Message}", ex);
        }
    }

    internal static MimeMessage BuildMessage(TenantMailSettings settings, EmailMessage message) {
        var mime = new MimeMessage();
        try {
            mime.From.Add(new MailboxAddress(settings.FromName ?? "", settings.FromAddress));
            foreach (var to in message.To) {
                mime.To.Add(MailboxAddress.Parse(to));
            }
        } catch (ParseException ex) {
            throw new EmailDeliveryException($"Invalid email address: {ex.Message}", ex);
        }

        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();
        return mime;
    }

    private static SecureSocketOptions ToSocketOptions(SmtpSecurity security) => security switch {
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        SmtpSecurity.None => SecureSocketOptions.None,
        _ => SecureSocketOptions.StartTls
    };

    private async Task<Socket> ConnectSocketAsync(string host, int port, CancellationToken cancellationToken) {
        var addresses = IPAddress.TryParse(host, out var literal)
            ? new[] { literal }
            : await Dns.GetHostAddressesAsync(host, cancellationToken);
        var allowed = _allowInternalHosts ? addresses : addresses.Where(a => !NetworkAddresses.IsInternal(a)).ToArray();
        if (allowed.Length == 0) {
            throw new EmailDeliveryException(
                $"{host} is on an internal network, and this server only sends mail through public mail servers. " +
                "Use your mail provider's SMTP host, or ask whoever runs this server to allow internal mail " +
                "servers (Email__AllowInternalHosts=true).");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeoutMilliseconds);
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try {
            await socket.ConnectAsync(allowed, port, timeout.Token);
            return socket;
        } catch {
            socket.Dispose();
            throw;
        }
    }
}
