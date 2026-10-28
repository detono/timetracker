using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Exceptions;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.MailSettings.Commands.SendTestEmail;
using TimeTracker.Application.MailSettings.Commands.UpdateMailSettings;
using TimeTracker.Application.MailSettings.Queries.GetMailSettings;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.MailSettings;

public class MailSettingsHandlerTests {
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITenantMailSettingsRepository> _repository = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMailDispatcher> _dispatcher = new();
    private readonly FakeProtector _protector = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    public MailSettingsHandlerTests() {
        _unitOfWork.SetupGet(u => u.MailSettings).Returns(_repository.Object);
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
    }

    private TestCurrentUserService Employer(Guid? userId = null) => new(userId ?? Guid.NewGuid(), UserRole.Employer, _tenantId);
    private TestCurrentUserService Employee() => new(Guid.NewGuid(), UserRole.Employee, _tenantId);

    private void GivenSettings(TenantMailSettings? settings) =>
        _repository.Setup(r => r.GetForCurrentTenantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

    private static UpdateMailSettingsCommand SmtpCommand(string? password = "pw") => new(
        MailProvider.Smtp, true, "noreply@acme.test", "Acme", "smtp-relay.brevo.com", 587, SmtpSecurity.StartTls,
        "user@acme.test", password, null, null, null, null);

    private static UpdateMailSettingsCommand GraphCommand(string? secret) => new(
        MailProvider.MicrosoftGraph, true, "hr@acme.test", null, null, 587, SmtpSecurity.StartTls, null, null,
        "acme.onmicrosoft.com", "client-id", secret, "hr@acme.test");

    private static TenantMailSettings StoredSmtp(string protectedPassword = "enc(old)") {
        var settings = TenantMailSettings.Create("noreply@acme.test", "Acme");
        settings.UseSmtp("smtp.acme.test", 587, SmtpSecurity.StartTls, "user@acme.test", protectedPassword);
        return settings;
    }

    // ---- Get ----

    [Fact]
    public async Task Get_AsEmployee_IsForbidden() {
        var result = await new GetMailSettingsQueryHandler(_unitOfWork.Object, Employee())
            .Handle(new GetMailSettingsQuery(), CancellationToken.None);

        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Get_NothingSaved_ReturnsNotConfiguredDefaults() {
        GivenSettings(null);

        var result = await new GetMailSettingsQueryHandler(_unitOfWork.Object, Employer())
            .Handle(new GetMailSettingsQuery(), CancellationToken.None);

        result.Value!.IsConfigured.Should().BeFalse();
        result.Value.IsEnabled.Should().BeFalse();
        result.Value.SmtpPort.Should().Be(587);
    }

    [Fact]
    public async Task Get_Saved_ReportsOnlyWhetherSecretIsSet() {
        GivenSettings(StoredSmtp());

        var result = await new GetMailSettingsQueryHandler(_unitOfWork.Object, Employer())
            .Handle(new GetMailSettingsQuery(), CancellationToken.None);

        result.Value!.IsConfigured.Should().BeTrue();
        result.Value.HasSmtpPassword.Should().BeTrue();
        result.Value.HasGraphClientSecret.Should().BeFalse();
        result.Value.GetType().GetProperties().Select(p => p.Name)
            .Should().NotContain(n => n.Contains("Protected") || n == "SmtpPassword" || n == "GraphClientSecret");
    }

    // ---- Update ----

    [Fact]
    public async Task Update_AsEmployee_IsForbidden() {
        var result = await new UpdateMailSettingsCommandHandler(_unitOfWork.Object, Employee(), _protector)
            .Handle(SmtpCommand(), CancellationToken.None);

        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_FirstTime_AddsSettingsWithEncryptedPassword() {
        GivenSettings(null);
        TenantMailSettings? added = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<TenantMailSettings>(), It.IsAny<CancellationToken>()))
            .Callback<TenantMailSettings, CancellationToken>((s, _) => added = s);

        var result = await new UpdateMailSettingsCommandHandler(_unitOfWork.Object, Employer(), _protector)
            .Handle(SmtpCommand("pw"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.HasSmtpPassword.Should().BeTrue();
        added.Should().NotBeNull();
        added!.SmtpPasswordProtected.Should().Be("enc(pw)");
        added.IsEnabled.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Update_EmptyPassword_KeepsStoredOne(string? password) {
        var stored = StoredSmtp("enc(old)");
        GivenSettings(stored);

        var result = await new UpdateMailSettingsCommandHandler(_unitOfWork.Object, Employer(), _protector)
            .Handle(SmtpCommand(password), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        stored.SmtpPasswordProtected.Should().Be("enc(old)");
        stored.SmtpHost.Should().Be("smtp-relay.brevo.com");
        _repository.Verify(r => r.AddAsync(It.IsAny<TenantMailSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_GraphWithoutAnySecret_Fails() {
        GivenSettings(null);

        var result = await new UpdateMailSettingsCommandHandler(_unitOfWork.Object, Employer(), _protector)
            .Handle(GraphCommand(secret: ""), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("client secret");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_SwitchToGraph_KeepsSmtpFieldsForLater() {
        var stored = StoredSmtp();
        GivenSettings(stored);

        var result = await new UpdateMailSettingsCommandHandler(_unitOfWork.Object, Employer(), _protector)
            .Handle(GraphCommand(secret: "graph-secret"), CancellationToken.None);

        result.Value!.Provider.Should().Be(MailProvider.MicrosoftGraph);
        stored.GraphClientSecretProtected.Should().Be("enc(graph-secret)");
        stored.SmtpPasswordProtected.Should().Be("enc(old)");
    }

    [Fact]
    public void Validator_RequiresProviderSpecificFields() {
        var validator = new UpdateMailSettingsCommandValidator();

        validator.Validate(SmtpCommand() with { SmtpHost = "" }).IsValid.Should().BeFalse();
        validator.Validate(GraphCommand("x") with { GraphMailbox = "" }).IsValid.Should().BeFalse();
        validator.Validate(SmtpCommand() with { FromAddress = "nope" }).IsValid.Should().BeFalse();
        validator.Validate(SmtpCommand()).IsValid.Should().BeTrue();
        validator.Validate(GraphCommand("x")).IsValid.Should().BeTrue();
    }

    // ---- Send test ----

    [Fact]
    public async Task SendTest_NothingSaved_Fails() {
        GivenSettings(null);

        var result = await new SendTestEmailCommandHandler(_unitOfWork.Object, Employer(), _dispatcher.Object)
            .Handle(new SendTestEmailCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _dispatcher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendTest_SendsToCallingEmployerEvenWhenDisabled() {
        var stored = StoredSmtp(); // IsEnabled is false by default
        GivenSettings(stored);
        var employer = User.Create("Kim", "Stevens", "kim@acme.test", "hash", UserRole.Employer, _tenantId);
        _users.Setup(u => u.GetByIdAsync(employer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employer);

        var result = await new SendTestEmailCommandHandler(_unitOfWork.Object, Employer(employer.Id), _dispatcher.Object)
            .Handle(new SendTestEmailCommand(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _dispatcher.Verify(d => d.DispatchAsync(stored,
            It.Is<EmailMessage>(m => m.To.SequenceEqual(new[] { "kim@acme.test" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendTest_DeliveryFails_ReturnsTheReason() {
        GivenSettings(StoredSmtp());
        var employer = User.Create("Kim", "Stevens", "kim@acme.test", "hash", UserRole.Employer, _tenantId);
        _users.Setup(u => u.GetByIdAsync(employer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employer);
        _dispatcher.Setup(d => d.DispatchAsync(It.IsAny<TenantMailSettings>(), It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("The SMTP server rejected the username or password."));

        var result = await new SendTestEmailCommandHandler(_unitOfWork.Object, Employer(employer.Id), _dispatcher.Object)
            .Handle(new SendTestEmailCommand(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Be("The SMTP server rejected the username or password.");
    }

    [Fact]
    public async Task SendTest_AsEmployee_IsForbidden() {
        var result = await new SendTestEmailCommandHandler(_unitOfWork.Object, Employee(), _dispatcher.Object)
            .Handle(new SendTestEmailCommand(), CancellationToken.None);

        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    private class FakeProtector : ISecretProtector {
        public string Protect(string plainText) => $"enc({plainText})";
        public string Unprotect(string protectedText) => protectedText[4..^1];
    }
}
