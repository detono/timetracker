using System.Net;
using Microsoft.AspNetCore.DataProtection;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Email;

namespace TimeTracker.Infrastructure.UnitTests.Email;

/// <summary>Records every request and answers with whatever the test configured.</summary>
public class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler {
    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = new();

    public static FakeHttpMessageHandler Returning(HttpStatusCode status, string? json = null) =>
        new(_ => new HttpResponseMessage(status) {
            Content = json is null ? null : new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return respond(request);
    }
}

public class FakeGraphTokenProvider : IGraphTokenProvider {
    public const string Token = "fake-access-token";
    public (string TenantId, string ClientId, string Secret)? LastCall { get; private set; }

    public Task<string> GetAccessTokenAsync(string tenantId, string clientId, string clientSecret, CancellationToken cancellationToken) {
        LastCall = (tenantId, clientId, clientSecret);
        return Task.FromResult(Token);
    }
}

public static class TestSettings {
    public static ISecretProtector NewProtector() =>
        new DataProtectionSecretProtector(new EphemeralDataProtectionProvider());

    public static TenantMailSettings Graph(ISecretProtector protector, string secret = "s3cret", string mailbox = "noreply@contoso.com") {
        var settings = TenantMailSettings.Create("noreply@contoso.com", "Contoso");
        settings.UseMicrosoftGraph("contoso.onmicrosoft.com", "11111111-2222-3333-4444-555555555555", mailbox,
            protector.Protect(secret));
        settings.SetEnabled(true);
        return settings;
    }

    public static TenantMailSettings Smtp(ISecretProtector protector, string host = "127.0.0.1", int port = 587) {
        var settings = TenantMailSettings.Create("noreply@example.com", "Example");
        settings.UseSmtp(host, port, SmtpSecurity.StartTls, "user", protector.Protect("password"));
        settings.SetEnabled(true);
        return settings;
    }
}
