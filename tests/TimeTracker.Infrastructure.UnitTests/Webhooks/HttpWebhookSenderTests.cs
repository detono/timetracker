using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Infrastructure.Services;
using TimeTracker.Infrastructure.UnitTests.Email;
using TimeTracker.Infrastructure.Webhooks;
using Xunit;

namespace TimeTracker.Infrastructure.UnitTests.Webhooks;

public class HttpWebhookSenderTests {
    private readonly ISecretProtector _protector = TestSettings.NewProtector();

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(5) };
    }

    private Task<WebhookCallResult> SendAsync(string url, bool allowInternal) {
        var sender = new HttpWebhookSender(
            new Factory(HttpWebhookSender.CreateHandler(new WebhookOptions { AllowInternalHosts = allowInternal })),
            _protector, new DateTimeProvider());
        var webhook = Webhook.Create("Test", url, new[] { "leave.approved" }, _protector.Protect("whsec_test"));
        var delivery = WebhookDelivery.Create(webhook.Id, Guid.NewGuid(), "leave.approved", "x", "{}", DateTime.UtcNow);
        return sender.SendAsync(webhook, delivery, CancellationToken.None);
    }

    [Theory]
    [InlineData("http://127.0.0.1:9/hook")]
    [InlineData("http://localhost:9/hook")]
    [InlineData("http://10.0.0.5/hook")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://[::1]:9/hook")]
    public async Task InternalAddresses_AreRefused_AndNotRetried(string url) {
        var result = await SendAsync(url, allowInternal: false);

        result.Succeeded.Should().BeFalse();
        result.Retry.Should().BeFalse();
        result.Error.Should().Contain("internal network").And.Contain("WEBHOOK_ALLOW_INTERNAL_HOSTS");
    }

    [Fact]
    public async Task Redirects_AreNotFollowed() {
        using var server = new OneResponseServer("HTTP/1.1 302 Found\r\nLocation: http://169.254.169.254/\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");

        var result = await SendAsync($"http://127.0.0.1:{server.Port}/hook", allowInternal: true);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(302);
        result.Error.Should().Contain("redirect");
    }

    [Fact]
    public async Task InternalAddresses_WorkWhenAllowed() {
        using var server = new OneResponseServer("HTTP/1.1 204 No Content\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");

        var result = await SendAsync($"http://127.0.0.1:{server.Port}/hook", allowInternal: true);

        result.Should().Be(WebhookCallResult.Success(204));
        server.Request.Should().Contain("POST /hook").And.Contain("X-Webhook-Signature: sha256=");
    }

    [Fact]
    public void Signature_MatchesOpenSsl() {
        // printf '1700000000.{"a":1}' | openssl dgst -sha256 -hmac whsec_test
        HttpWebhookSender.Sign("whsec_test", "1700000000", "{\"a\":1}")
            .Should().Be("38877139021993b830af32feea6e18a8da83eb2f6e49ee50bd9e4cf4ca4d3789");
    }

    /// <summary>Answers one HTTP request on a free local port with a fixed response.</summary>
    private sealed class OneResponseServer : IDisposable {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly Task _serving;

        public OneResponseServer(string response) {
            _listener.Start();
            _serving = Task.Run(async () => {
                using var client = await _listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                var buffer = new byte[8192];
                var read = await stream.ReadAsync(buffer);
                Request = Encoding.UTF8.GetString(buffer, 0, read);
                await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
            });
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public string Request { get; private set; } = "";

        public void Dispose() {
            _listener.Stop();
            try { _serving.Wait(TimeSpan.FromSeconds(1)); } catch (AggregateException) { }
        }
    }
}
