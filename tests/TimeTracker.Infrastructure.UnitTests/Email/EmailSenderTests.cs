using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using TimeTracker.Infrastructure.Email;
using Xunit;

namespace TimeTracker.Infrastructure.UnitTests.Email;

public class EmailSenderTests {
    private readonly Mock<ITenantMailSettingsRepository> _repository = new();
    private readonly Mock<IMailDispatcher> _dispatcher = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly EmailMessage _message = new(new[] { "kim@example.com" }, "Hi", "<p>Hi</p>");

    private EmailSender Sender() =>
        new(_repository.Object, _dispatcher.Object, _currentUser.Object, NullLogger<EmailSender>.Instance);

    private void GivenSettings(TenantMailSettings? settings) =>
        _repository.Setup(r => r.GetForCurrentTenantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

    [Fact]
    public async Task SendAsync_NoSettings_SkipsWithoutThrowing() {
        GivenSettings(null);

        var result = await Sender().SendAsync(_message);

        result.Status.Should().Be(EmailSendStatus.NotConfigured);
        _dispatcher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_Disabled_SkipsWithoutThrowing() {
        var settings = TestSettings.Smtp(TestSettings.NewProtector());
        settings.SetEnabled(false);
        GivenSettings(settings);

        var result = await Sender().SendAsync(_message);

        result.Status.Should().Be(EmailSendStatus.NotConfigured);
        _dispatcher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_Enabled_DispatchesWithTenantSettings() {
        var settings = TestSettings.Smtp(TestSettings.NewProtector());
        GivenSettings(settings);

        var result = await Sender().SendAsync(_message);

        result.Status.Should().Be(EmailSendStatus.Sent);
        _dispatcher.Verify(d => d.DispatchAsync(settings, _message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_DeliveryFails_ReturnsFailureInsteadOfThrowing() {
        GivenSettings(TestSettings.Smtp(TestSettings.NewProtector()));
        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<TenantMailSettings>(), _message, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("bad password"));

        var result = await Sender().SendAsync(_message);

        result.Should().Be(new EmailSendResult(EmailSendStatus.Failed, "bad password"));
    }

    [Fact]
    public async Task Dispatcher_RejectsMessageWithoutRecipients() {
        var protector = TestSettings.NewProtector();
        var dispatcher = new MailDispatcher(new SmtpMailTransport(protector),
            new GraphMailTransport(new HttpClient(), new FakeGraphTokenProvider(), protector));

        var act = () => dispatcher.DispatchAsync(TestSettings.Smtp(protector), _message with { To = Array.Empty<string>() });

        await act.Should().ThrowAsync<EmailDeliveryException>().WithMessage("*no valid recipient*");
    }

    [Fact]
    public void SecretProtector_RoundTripsWithoutStoringPlainText() {
        var protector = TestSettings.NewProtector();

        var cipher = protector.Protect("hunter2");

        cipher.Should().NotContain("hunter2");
        protector.Unprotect(cipher).Should().Be("hunter2");
    }

    [Fact]
    public void Settings_SwitchingSmtpToNoLogin_DropsStoredPassword() {
        var settings = TestSettings.Smtp(TestSettings.NewProtector());

        settings.UseSmtp("relay.local", 25, SmtpSecurity.None, username: "", protectedPassword: null);

        settings.SmtpUsername.Should().BeNull();
        settings.SmtpPasswordProtected.Should().BeNull();
    }
}
