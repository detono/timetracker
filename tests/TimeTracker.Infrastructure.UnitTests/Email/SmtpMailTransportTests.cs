using System.Net;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using FluentAssertions;
using MimeKit;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Models;
using TimeTracker.Infrastructure.Email;
using Xunit;

namespace TimeTracker.Infrastructure.UnitTests.Email;

public class SmtpMailTransportTests {
    // These tests talk to a local fake server, which production blocks unless allowed.
    private static readonly IOptions<MailNetworkOptions> AllowInternalHosts =
        Options.Create(new MailNetworkOptions { AllowInternalHosts = true });

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

        var act = () => new SmtpMailTransport(protector, AllowInternalHosts)
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

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.20.0.5")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]
    [InlineData("::1")]
    [InlineData("localhost")]
    public async Task InternalHosts_AreRefusedByDefault(string host) {
        var protector = TestSettings.NewProtector();
        var act = () => new SmtpMailTransport(protector)
            .SendAsync(TestSettings.Smtp(protector, host, 25), Message, CancellationToken.None);

        (await act.Should().ThrowAsync<EmailDeliveryException>())
            .Which.Message.Should().Contain("internal network").And.Contain("Email__AllowInternalHosts");
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.15.255.255", false)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("172.32.0.1", false)]
    [InlineData("192.168.0.1", true)]
    [InlineData("169.254.169.254", true)]
    [InlineData("100.64.0.1", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("224.0.0.1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("1.1.1.1", false)]
    [InlineData("::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("fd00::1", true)]
    [InlineData("::ffff:10.0.0.1", true)]
    [InlineData("2001:4860:4860::8888", false)]
    public void IsInternal_RecognisesPrivateAndSpecialAddresses(string address, bool expected) {
        NetworkAddresses.IsInternal(IPAddress.Parse(address)).Should().Be(expected);
    }
}
