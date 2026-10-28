using System.Net;
using System.Text.Json;
using FluentAssertions;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Models;
using TimeTracker.Infrastructure.Email;
using Xunit;

namespace TimeTracker.Infrastructure.UnitTests.Email;

/// <summary>
/// Exercises the Graph sendMail call against a fake HTTP handler. Nothing here talks to a
/// real Microsoft 365 tenant, so token acquisition and Graph's actual behaviour aren't covered.
/// </summary>
public class GraphMailTransportTests {
    private readonly FakeGraphTokenProvider _tokens = new();
    private readonly Application.Common.Interfaces.ISecretProtector _protector = TestSettings.NewProtector();

    private static readonly EmailMessage Message = new(
        new[] { "kim@contoso.com", "lee@contoso.com" }, "Leave request", "<p>Hello</p>", "Hello");

    private GraphMailTransport Transport(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), _tokens, _protector);

    [Fact]
    public async Task SendAsync_PostsSendMailForTheMailboxWithBearerToken() {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.Accepted);
        var settings = TestSettings.Graph(_protector, secret: "the-secret");

        await Transport(handler).SendAsync(settings, Message, CancellationToken.None);

        _tokens.LastCall.Should().Be(("contoso.onmicrosoft.com", "11111111-2222-3333-4444-555555555555", "the-secret"));

        var (request, body) = handler.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.AbsoluteUri.Should().Be("https://graph.microsoft.com/v1.0/users/noreply%40contoso.com/sendMail");
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be(FakeGraphTokenProvider.Token);

        using var json = JsonDocument.Parse(body!);
        var message = json.RootElement.GetProperty("message");
        message.GetProperty("subject").GetString().Should().Be("Leave request");
        message.GetProperty("body").GetProperty("contentType").GetString().Should().Be("HTML");
        message.GetProperty("body").GetProperty("content").GetString().Should().Be("<p>Hello</p>");
        message.GetProperty("from").GetProperty("emailAddress").GetProperty("name").GetString().Should().Be("Contoso");
        message.GetProperty("toRecipients").EnumerateArray()
            .Select(r => r.GetProperty("emailAddress").GetProperty("address").GetString())
            .Should().Equal("kim@contoso.com", "lee@contoso.com");
        json.RootElement.GetProperty("saveToSentItems").GetBoolean().Should().BeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "401")]
    [InlineData(HttpStatusCode.Forbidden, "403")]
    public async Task SendAsync_AuthorizationErrors_HintAtMailSendApplicationPermission(HttpStatusCode status, string code) {
        var handler = FakeHttpMessageHandler.Returning(status,
            """{"error":{"code":"ErrorAccessDenied","message":"Access is denied. Check credentials and try again."}}""");

        var act = () => Transport(handler).SendAsync(TestSettings.Graph(_protector), Message, CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<EmailDeliveryException>()).Which;
        ex.Message.Should().Contain(code)
            .And.Contain("Mail.Send")
            .And.Contain("Application")
            .And.Contain("ErrorAccessDenied")
            .And.Contain("noreply@contoso.com");
    }

    [Fact]
    public async Task SendAsync_UnknownMailbox_SaysSo() {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.NotFound,
            """{"error":{"code":"ErrorInvalidUser","message":"The requested user is invalid."}}""");

        var act = () => Transport(handler).SendAsync(TestSettings.Graph(_protector, mailbox: "ghost@contoso.com"), Message,
            CancellationToken.None);

        (await act.Should().ThrowAsync<EmailDeliveryException>()).Which.Message
            .Should().Contain("ghost@contoso.com").And.Contain("404").And.Contain("ErrorInvalidUser");
    }

    [Fact]
    public async Task SendAsync_OtherErrorWithoutJsonBody_ReportsStatus() {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) {
            Content = new StringContent("<html>down</html>")
        });

        var act = () => Transport(handler).SendAsync(TestSettings.Graph(_protector), Message, CancellationToken.None);

        (await act.Should().ThrowAsync<EmailDeliveryException>()).Which.Message.Should().Contain("503");
    }

    [Fact]
    public async Task SendAsync_NetworkFailure_IsReportedAsUnreachable() {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("No such host is known."));

        var act = () => Transport(handler).SendAsync(TestSettings.Graph(_protector), Message, CancellationToken.None);

        (await act.Should().ThrowAsync<EmailDeliveryException>()).Which.Message
            .Should().Contain("Could not reach Microsoft Graph");
    }

    [Fact]
    public async Task SendAsync_SecretEncryptedWithOtherKeys_AsksToReenterIt() {
        var settings = TestSettings.Graph(TestSettings.NewProtector()); // different key ring
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.Accepted);

        var act = () => Transport(handler).SendAsync(settings, Message, CancellationToken.None);

        (await act.Should().ThrowAsync<EmailDeliveryException>()).Which.Message.Should().Contain("Enter it again");
        handler.Requests.Should().BeEmpty();
    }
}
