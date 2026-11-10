using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Application.HourTypes.Dtos;
using TimeTracker.Application.LeaveRequests.Dtos;
using TimeTracker.Application.Users.Dtos;
using TimeTracker.Application.Webhooks;
using TimeTracker.Domain.Enums;
using TimeTracker.Infrastructure.Webhooks;
using Xunit;
using static TimeTracker.API.IntegrationTests.TestApi;

namespace TimeTracker.API.IntegrationTests;

/// <summary>Stands in for the receiving system: records every call and answers with <see cref="Status"/>.</summary>
public class FakeReceiver : HttpMessageHandler {
    public record Call(Uri Url, Dictionary<string, string> Headers, string Body);

    public List<Call> Calls { get; } = new();
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var headers = request.Headers.Concat(request.Content!.Headers)
            .ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        var body = await request.Content.ReadAsStringAsync(cancellationToken);
        lock (Calls) {
            Calls.Add(new Call(request.RequestUri!, headers, body));
        }

        return new HttpResponseMessage(Status) { Content = new StringContent(Status == HttpStatusCode.OK ? "" : "nope") };
    }
}

/// <summary>Webhooks call <see cref="Receiver"/> instead of the network, and only when a test sends them.</summary>
public class WebhookFactory : CustomWebApplicationFactory {
    public FakeReceiver Receiver { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Webhooks:BackgroundSending"] = "false" }));
        builder.ConfigureServices(services =>
            services.AddHttpClient(HttpWebhookSender.ClientName).ConfigurePrimaryHttpMessageHandler(() => Receiver));
    }

    public Task<int> SendDueAsync() => Services.GetRequiredService<WebhookDispatcher>().SendDueAsync(CancellationToken.None);
}

public class WebhookTests(WebhookFactory factory) : IClassFixture<WebhookFactory> {
    private const string Monday = "2026-11-02";
    private const string Tuesday = "2026-11-03";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) {
        using var request = Authorized(method, url, token);
        if (body is not null) {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return await _client.SendAsync(request);
    }

    private async Task<T> OkAsync<T>(HttpMethod method, string url, string token, object? body = null) {
        var response = await SendAsync(method, url, token, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<(string EmployerToken, Guid EmployeeId, string EmployeeToken)> CompanyAsync() {
        var tenant = await CreateTenantAsync(_client);
        var employerToken = await LoginAsync(_client, tenant.EmployerEmail, Password);
        var (employeeId, _, employeeToken) = await CreateEmployeeAsync(_client, employerToken, "Jonathan", "Peeters");
        return (employerToken, employeeId, employeeToken);
    }

    private Task<WebhookDto> CreateWebhookAsync(string token, params string[] events) =>
        OkAsync<WebhookDto>(HttpMethod.Post, "/api/webhooks", token, new {
            Name = "Payroll", Url = $"https://receiver.example/{Guid.NewGuid()}", Events = events, IsEnabled = true,
            HeaderName = "Authorization", HeaderValue = "Bearer top-secret-key"
        });

    private async Task<LeaveRequestDto> RequestLeaveAsync(string employeeToken, string employerToken) {
        var paidLeave = (await OkAsync<List<HourTypeDto>>(HttpMethod.Get, "/api/hourtypes", employerToken))
            .Single(t => t.DeductsFromLeaveBalance);
        return await OkAsync<LeaveRequestDto>(HttpMethod.Post, "/api/leave-requests", employeeToken, new {
            HourTypeId = paidLeave.Id, StartDate = Monday, EndDate = Tuesday, Comment = "Family trip"
        });
    }

    private List<FakeReceiver.Call> CallsTo(WebhookDto webhook) {
        lock (factory.Receiver.Calls) {
            return factory.Receiver.Calls.Where(c => c.Url.ToString() == webhook.Url).ToList();
        }
    }

    [Fact]
    public async Task ApprovedLeave_CallsTheWebhook_SignedAndWithExternalIds() {
        var (employerToken, employeeId, employeeToken) = await CompanyAsync();
        (await SendAsync(HttpMethod.Put, $"/api/users/{employeeId}/external-id", employerToken, new { ExternalId = " EMP-0042 " }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var paidLeave = (await OkAsync<List<HourTypeDto>>(HttpMethod.Get, "/api/hourtypes", employerToken)).Single(t => t.DeductsFromLeaveBalance);
        await OkAsync<HourTypeDto>(HttpMethod.Put, $"/api/hourtypes/{paidLeave.Id}", employerToken, new {
            paidLeave.Id, paidLeave.LocalizedNames, paidLeave.ColorHex, paidLeave.IsDefault, paidLeave.IsLeave,
            paidLeave.DeductsFromLeaveBalance, ExternalId = "PTO"
        });
        var webhook = await CreateWebhookAsync(employerToken, WebhookEvents.LeaveApproved);

        var leave = await RequestLeaveAsync(employeeToken, employerToken);
        await factory.SendDueAsync();
        CallsTo(webhook).Should().BeEmpty("the webhook doesn't listen to leave.requested");

        await OkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{leave.Id}/approve", employerToken, new { Comment = (string?)null });
        CallsTo(webhook).Should().BeEmpty("calls are sent in the background, not during the approval");
        await factory.SendDueAsync();

        var call = CallsTo(webhook).Single();
        call.Headers["X-Webhook-Event"].Should().Be("leave.approved");
        call.Headers["Authorization"].Should().Be("Bearer top-secret-key");
        call.Headers["Content-Type"].Should().StartWith("application/json");

        var secret = (await OkAsync<WebhookSecretDto>(HttpMethod.Get, $"/api/webhooks/{webhook.Id}/secret", employerToken)).Secret;
        secret.Should().StartWith("whsec_");
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes($"{call.Headers["X-Webhook-Timestamp"]}.{call.Body}"))).ToLowerInvariant();
        call.Headers["X-Webhook-Signature"].Should().Be($"sha256={expected}");

        using var body = JsonDocument.Parse(call.Body);
        var root = body.RootElement;
        root.GetProperty("id").GetString().Should().Be(call.Headers["X-Webhook-Id"]);
        root.GetProperty("event").GetString().Should().Be("leave.approved");
        root.GetProperty("employee").GetProperty("externalId").GetString().Should().Be("EMP-0042");
        root.GetProperty("employee").GetProperty("firstName").GetString().Should().Be("Jonathan");
        root.GetProperty("actor").GetProperty("name").GetString().Should().Be("Test Employer");
        var leaveJson = root.GetProperty("leave");
        leaveJson.GetProperty("id").GetGuid().Should().Be(leave.Id);
        leaveJson.GetProperty("status").GetString().Should().Be("Approved");
        leaveJson.GetProperty("startDate").GetString().Should().Be(Monday);
        leaveJson.GetProperty("endDate").GetString().Should().Be(Tuesday);
        leaveJson.GetProperty("hours").GetDecimal().Should().BeGreaterThan(0);
        leaveJson.GetProperty("comment").GetString().Should().Be("Family trip");
        leaveJson.GetProperty("type").GetProperty("externalId").GetString().Should().Be("PTO");
        var days = leaveJson.GetProperty("days").EnumerateArray().ToList();
        days.Select(d => d.GetProperty("date").GetString()).Should().Equal(Monday, Tuesday);
        days.Should().AllSatisfy(d => TimeOnly.Parse(d.GetProperty("startTime").GetString()!)
            .Should().BeBefore(TimeOnly.Parse(d.GetProperty("endTime").GetString()!)));

        var overview = await OkAsync<WebhooksOverviewDto>(HttpMethod.Get, "/api/webhooks", employerToken);
        var delivery = overview.RecentDeliveries.Single(d => d.WebhookId == webhook.Id);
        delivery.Status.Should().Be(WebhookDeliveryStatus.Succeeded);
        delivery.Summary.Should().Be($"Jonathan Peeters · {Monday} – {Tuesday}");
        overview.AvailableEvents.Should().Equal(WebhookEvents.All);
    }

    [Fact]
    public async Task EveryLeaveChange_HasItsOwnEvent() {
        var (employerToken, _, employeeToken) = await CompanyAsync();
        var webhook = await CreateWebhookAsync(employerToken, WebhookEvents.All.ToArray());

        var first = await RequestLeaveAsync(employeeToken, employerToken);
        await OkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{first.Id}/reject", employerToken, new { Comment = "Busy week" });
        var second = await RequestLeaveAsync(employeeToken, employerToken);
        await OkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{second.Id}/approve", employerToken, new { Comment = (string?)null });
        await OkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{second.Id}/cancel", employerToken, new { Comment = (string?)null });
        await factory.SendDueAsync();

        CallsTo(webhook).Select(c => c.Headers["X-Webhook-Event"]).Should().BeEquivalentTo(new[] {
            "leave.requested", "leave.rejected", "leave.requested", "leave.approved", "leave.cancelled"
        });
    }

    [Fact]
    public async Task OnlyApprovedLeave_ListsTheBookedDays() {
        var (employerToken, _, employeeToken) = await CompanyAsync();
        var webhook = await CreateWebhookAsync(employerToken, WebhookEvents.All.ToArray());

        var leave = await RequestLeaveAsync(employeeToken, employerToken);
        await OkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{leave.Id}/approve", employerToken, new { Comment = (string?)null });
        await OkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{leave.Id}/cancel", employerToken, new { Comment = (string?)null });
        await factory.SendDueAsync();

        var daysByEvent = CallsTo(webhook).ToDictionary(
            c => c.Headers["X-Webhook-Event"],
            c => JsonDocument.Parse(c.Body).RootElement.GetProperty("leave").GetProperty("days").ValueKind);
        daysByEvent.Should().BeEquivalentTo(new Dictionary<string, JsonValueKind> {
            ["leave.requested"] = JsonValueKind.Null,
            ["leave.approved"] = JsonValueKind.Array,
            ["leave.cancelled"] = JsonValueKind.Null
        });
    }

    [Fact]
    public async Task ApprovedLeaveOverAWeekend_LeavesTheWeekendOut() {
        var (employerToken, _, employeeToken) = await CompanyAsync();
        var webhook = await CreateWebhookAsync(employerToken, WebhookEvents.LeaveApproved);
        var paidLeave = (await OkAsync<List<HourTypeDto>>(HttpMethod.Get, "/api/hourtypes", employerToken)).Single(t => t.DeductsFromLeaveBalance);

        // Friday to Monday: Saturday and Sunday aren't working days.
        var leave = await OkAsync<LeaveRequestDto>(HttpMethod.Post, "/api/leave-requests", employeeToken, new {
            HourTypeId = paidLeave.Id, StartDate = "2026-11-06", EndDate = "2026-11-09", Comment = (string?)null
        });
        await OkAsync<LeaveRequestDto>(HttpMethod.Post, $"/api/leave-requests/{leave.Id}/approve", employerToken, new { Comment = (string?)null });
        await factory.SendDueAsync();

        using var body = JsonDocument.Parse(CallsTo(webhook).Single().Body);
        body.RootElement.GetProperty("leave").GetProperty("days").EnumerateArray()
            .Select(d => d.GetProperty("date").GetString())
            .Should().Equal("2026-11-06", "2026-11-09");
    }

    [Fact]
    public async Task FailedCalls_AreRetriedLater_UnlessRetryingCantHelp_AndCanBeSentAgain() {
        var (employerToken, _, employeeToken) = await CompanyAsync();
        var webhook = await CreateWebhookAsync(employerToken, WebhookEvents.LeaveRequested);

        factory.Receiver.Status = HttpStatusCode.ServiceUnavailable;
        try {
            await RequestLeaveAsync(employeeToken, employerToken);
            await factory.SendDueAsync();
            var delivery = (await OkAsync<WebhooksOverviewDto>(HttpMethod.Get, "/api/webhooks", employerToken))
                .RecentDeliveries.Single(d => d.WebhookId == webhook.Id);
            delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
            delivery.Attempts.Should().Be(1);
            delivery.LastStatusCode.Should().Be(503);
            delivery.NextAttemptAtUtc.Should().BeAfter(DateTime.UtcNow.AddSeconds(30));

            await factory.SendDueAsync();
            CallsTo(webhook).Should().HaveCount(1, "the retry isn't due yet");
        } finally {
            factory.Receiver.Status = HttpStatusCode.OK;
        }

        var other = await CreateWebhookAsync(employerToken, WebhookEvents.LeaveRequested);
        factory.Receiver.Status = HttpStatusCode.BadRequest;
        try {
            var (_, _, colleagueToken) = await CreateEmployeeAsync(_client, employerToken);
            await RequestLeaveAsync(colleagueToken, employerToken);
            await factory.SendDueAsync();
        } finally {
            factory.Receiver.Status = HttpStatusCode.OK;
        }

        var failed = (await OkAsync<WebhooksOverviewDto>(HttpMethod.Get, "/api/webhooks", employerToken))
            .RecentDeliveries.Single(d => d.WebhookId == other.Id);
        failed.Status.Should().Be(WebhookDeliveryStatus.Failed, "a 400 won't go away by waiting");
        failed.LastError.Should().Contain("400").And.Contain("nope");

        (await SendAsync(HttpMethod.Post, $"/api/webhooks/deliveries/{failed.Id}/retry", employerToken))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        await factory.SendDueAsync();
        (await OkAsync<WebhooksOverviewDto>(HttpMethod.Get, "/api/webhooks", employerToken))
            .RecentDeliveries.Single(d => d.Id == failed.Id).Status.Should().Be(WebhookDeliveryStatus.Succeeded);
    }

    [Fact]
    public async Task SwitchedOffWebhooks_GetNothing() {
        var (employerToken, _, employeeToken) = await CompanyAsync();
        var webhook = await CreateWebhookAsync(employerToken, WebhookEvents.LeaveRequested);
        await OkAsync<WebhookDto>(HttpMethod.Put, $"/api/webhooks/{webhook.Id}", employerToken, new {
            webhook.Name, webhook.Url, webhook.Events, IsEnabled = false, webhook.HeaderName, HeaderValue = ""
        });

        await RequestLeaveAsync(employeeToken, employerToken);
        await factory.SendDueAsync();

        CallsTo(webhook).Should().BeEmpty();
    }

    [Fact]
    public async Task SendTest_CallsRightAway_AndSecretsNeverShowInTheList() {
        var (employerToken, _, _) = await CompanyAsync();
        var webhook = await CreateWebhookAsync(employerToken, WebhookEvents.LeaveApproved);

        var result = await OkAsync<WebhookDeliveryDto>(HttpMethod.Post, $"/api/webhooks/{webhook.Id}/test", employerToken);

        result.Status.Should().Be(WebhookDeliveryStatus.Succeeded);
        CallsTo(webhook).Single().Headers["X-Webhook-Event"].Should().Be("webhook.test");
        webhook.HasHeaderValue.Should().BeTrue();
        var list = await (await SendAsync(HttpMethod.Get, "/api/webhooks", employerToken)).Content.ReadAsStringAsync();
        var secret = (await OkAsync<WebhookSecretDto>(HttpMethod.Get, $"/api/webhooks/{webhook.Id}/secret", employerToken)).Secret;
        list.Should().NotContain(secret).And.NotContain("top-secret-key");

        var rotated = await OkAsync<WebhookSecretDto>(HttpMethod.Post, $"/api/webhooks/{webhook.Id}/secret", employerToken);
        rotated.Secret.Should().NotBe(secret);
    }

    [Fact]
    public async Task OnlyEmployersOfTheCompany_CanSeeOrChangeWebhooks() {
        var (employerToken, _, employeeToken) = await CompanyAsync();
        var webhook = await CreateWebhookAsync(employerToken, WebhookEvents.LeaveApproved);
        var (otherEmployerToken, _, _) = await CompanyAsync();

        (await SendAsync(HttpMethod.Get, "/api/webhooks", employeeToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Get, $"/api/webhooks/{webhook.Id}/secret", employeeToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(HttpMethod.Get, $"/api/webhooks/{webhook.Id}/secret", otherEmployerToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(HttpMethod.Delete, $"/api/webhooks/{webhook.Id}", otherEmployerToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await OkAsync<WebhooksOverviewDto>(HttpMethod.Get, "/api/webhooks", otherEmployerToken)).Webhooks.Should().BeEmpty();

        (await SendAsync(HttpMethod.Delete, $"/api/webhooks/{webhook.Id}", employerToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await OkAsync<WebhooksOverviewDto>(HttpMethod.Get, "/api/webhooks", employerToken)).Webhooks.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ftp://receiver.example/x", "leave.approved", null)]
    [InlineData("https://user:pass@receiver.example/x", "leave.approved", null)]
    [InlineData("https://receiver.example/x", "leave.exploded", null)]
    [InlineData("https://receiver.example/x", "leave.approved", "Host")]
    [InlineData("https://receiver.example/x", "leave.approved", "X-Webhook-Signature")]
    [InlineData("https://receiver.example/x", "leave.approved", "Bad Header")]
    public async Task InvalidWebhooks_AreRefused(string url, string eventType, string? headerName) {
        var (employerToken, _, _) = await CompanyAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/webhooks", employerToken, new {
            Name = "Payroll", Url = url, Events = new[] { eventType }, IsEnabled = true, HeaderName = headerName, HeaderValue = "x"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExternalIds_AreUniqueInTheCompany_AndCanBeCleared() {
        var (employerToken, employeeId, _) = await CompanyAsync();
        var (colleagueId, _, _) = await CreateEmployeeAsync(_client, employerToken);

        (await SendAsync(HttpMethod.Put, $"/api/users/{employeeId}/external-id", employerToken, new { ExternalId = "EMP-1" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(HttpMethod.Put, $"/api/users/{colleagueId}/external-id", employerToken, new { ExternalId = "emp-1" }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await SendAsync(HttpMethod.Put, $"/api/users/{employeeId}/external-id", employerToken, new { ExternalId = "  " }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await OkAsync<List<UserDto>>(HttpMethod.Get, "/api/users", employerToken))
            .Single(u => u.Id == employeeId).ExternalId.Should().BeNull();
    }
}
