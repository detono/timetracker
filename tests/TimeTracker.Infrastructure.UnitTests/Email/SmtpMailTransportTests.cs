using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using MimeKit;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Models;
using TimeTracker.Infrastructure.Email;
using Xunit;

namespace TimeTracker.Infrastructure.UnitTests.Email;

public class SmtpMailTransportTests {
    private static readonly EmailMessage Message = new(new[] { "kim@example.com" }, "Hi", "<p>Hi</p>", "Hi");

    [Fact]
    public void BuildMessage_HasSenderRecipientsAndBothBodies() {
        var settings = TestSettings.Smtp(TestSettings.NewProtector());

        var mime = SmtpMailTransport.BuildMessage(settings, Message);

        mime.From.Mailboxes.Should().ContainSingle().Which.Should()
            .Match<MailboxAddress>(m => m.Address == "noreply@example.com" && m.Name == "Example");
        mime.To.Mailboxes.Select(m => m.Address).Should().Equal("kim@example.com");
        mime.HtmlBody.Should().Be("<p>Hi</p>");
        mime.TextBody.Should().Be("Hi");
    }

    [Fact]
    public void BuildMessage_InvalidRecipient_ThrowsDeliveryException() {
        var settings = TestSettings.Smtp(TestSettings.NewProtector());

        var act = () => SmtpMailTransport.BuildMessage(settings, Message with { To = new[] { "not an address" } });

        act.Should().Throw<EmailDeliveryException>().WithMessage("Invalid email address*");
    }

    [Fact]
    public async Task SendAsync_NothingListening_ReportsHostUnreachable() {
        var port = GetClosedPort();
        var protector = TestSettings.NewProtector();

        var act = () => new SmtpMailTransport(protector)
            .SendAsync(TestSettings.Smtp(protector, "127.0.0.1", port), Message, CancellationToken.None);

        (await act.Should().ThrowAsync<EmailDeliveryException>()).Which.Message
            .Should().StartWith($"Could not connect to 127.0.0.1:{port}");
    }

    private static int GetClosedPort() {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
