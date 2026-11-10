using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Localization;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Leave;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.Localization;

public class EmailTextsTests {
    [Fact]
    public void ShipsEnglishDutchAndFrench() {
        EmailTexts.AvailableLanguages.Should().Contain(new[] { "en", "nl", "fr" });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("de")] // no file (yet)
    [InlineData("xx")]
    public void UnknownOrMissingLanguage_FallsBackToEnglish(string? language) {
        var texts = EmailTexts.For(language);

        texts.Language.Should().Be("en");
        texts.Get("greeting", ("firstName", "Eva")).Should().Be("Hi Eva,");
    }

    [Fact]
    public void FillsPlaceholdersInTheChosenLanguage() {
        EmailTexts.For("NL").Get("greeting", ("firstName", "Eva")).Should().Be("Hallo Eva,");
    }

    [Fact]
    public void EveryLanguageHasOnlyKeysEnglishHas() {
        var english = EmailTexts.Files["en"].Keys.ToHashSet();
        foreach (var (language, texts) in EmailTexts.Files) {
            texts.Keys.Should().BeSubsetOf(english, $"{language}.json should only translate keys from en.json");
        }
    }

    [Fact]
    public void NoTextHardcodesTheAppName() {
        foreach (var (language, texts) in EmailTexts.Files) {
            texts.Values.Should().NotContain(t => t.Contains("TimeTracker"), $"{language}.json should use {{app}}");
        }
    }

    [Fact]
    public async Task LeaveRequestEmail_WithoutAppUrl_TellsToOpenTheAppByItsName() {
        var (notifications, sent, request, requester, type) = LeaveRequestSetup(new TestBranding("Shiftlog"));

        await notifications.RequestSubmittedAsync(request, requester, type, CancellationToken.None);

        var email = sent.Single();
        email.TextBody.Should().Contain("Open Shiftlog and go to Leave");
        email.HtmlBody.Should().NotContain("<a ");
    }

    [Fact]
    public async Task LeaveRequestEmail_WithAppUrl_HasBrandedApproveAndRejectButtons() {
        var (notifications, sent, request, requester, type) =
            LeaveRequestSetup(new TestBranding("Shiftlog", "https://hours.example.com"));

        await notifications.RequestSubmittedAsync(request, requester, type, CancellationToken.None);

        var email = sent.Single();
        var approveUrl = $"https://hours.example.com/leave?review={request.Id}&decision=approve";
        var rejectUrl = $"https://hours.example.com/leave?review={request.Id}&decision=reject";
        email.HtmlBody.Should()
            .Contain($"href=\"{approveUrl.Replace("&", "&amp;")}\"")
            .And.Contain($"href=\"{rejectUrl.Replace("&", "&amp;")}\"")
            .And.Contain("background-color:#123456").And.Contain("color:#ffffff")
            .And.Contain("background-color:#abcdef").And.Contain("color:#000000")
            .And.Contain(">Approve</a>").And.Contain(">Reject</a>")
            .And.NotContain("Open Shiftlog");
        email.TextBody.Should().Contain($"Approve: {approveUrl}").And.Contain($"Reject: {rejectUrl}");
    }

    private static (LeaveNotifications, List<EmailMessage>, LeaveRequest, User, HourType) LeaveRequestSetup(TestBranding branding) {
        var company = new FakeCompany();
        var supervisor = company.Add("Sam", "Lead", UserRole.Employee);
        var requester = company.Add("Eva", "Peeters", UserRole.Employee);
        requester.AssignSupervisor(supervisor.Id);
        var type = PaidLeave();
        var request = LeaveRequest.Create(requester.Id, type.Id, new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4), 7.5m, null, DayPart.Full);
        return (company.Notifications(branding), company.Sent, request, requester, type);
    }

    private static HourType PaidLeave() =>
        HourType.Create(new Dictionary<string, string> { ["en"] = "Paid time off", ["nl"] = "Verlof", ["fr"] = "Congé payé" }, "#2a7ab0", false);

    /// <summary>A company with its people, and the emails sent to them.</summary>
    private sealed class FakeCompany {
        public Tenant Tenant { get; } = Tenant.Create("Acme");
        public List<User> Users { get; } = new();
        public List<EmailMessage> Sent { get; } = new();

        public User Add(string firstName, string lastName, UserRole role, string? language = null) {
            var user = User.Create(firstName, lastName, $"{firstName.ToLowerInvariant()}@co.com", "hash", role, Tenant.Id);
            user.SetLanguage(language);
            Users.Add(user);
            return user;
        }

        public LeaveNotifications Notifications(TestBranding? branding = null) {
            var users = new Mock<IUserRepository>();
            users.Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => Users.SingleOrDefault(u => u.Id == id));
            users.Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => Users.ToList());
            var tenants = new Mock<ITenantRepository>();
            tenants.Setup(t => t.GetByIdAsync(Tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Tenant);
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.SetupGet(u => u.Users).Returns(users.Object);
            unitOfWork.SetupGet(u => u.Tenants).Returns(tenants.Object);
            var sender = new Mock<IEmailSender>();
            sender.Setup(s => s.SendAsync(Capture.In(Sent), It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmailSendResult.Sent());
            return new LeaveNotifications(unitOfWork.Object, sender.Object, branding ?? new TestBranding("Shiftlog", "https://hours.example.com"));
        }
    }

    private static async Task RequestAsync(FakeCompany company, User requester) {
        var request = LeaveRequest.Create(requester.Id, Guid.NewGuid(), new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 12), 8.25m, "Dentist");
        await company.Notifications().RequestSubmittedAsync(request, requester, PaidLeave(), CancellationToken.None);
    }

    [Fact]
    public async Task LeaveRequest_GoesToTheSupervisor_NotTheEmployers() {
        var company = new FakeCompany();
        company.Add("Boss", "Owner", UserRole.Employer);
        var supervisor = company.Add("Marcella", "Lead", UserRole.Employee);
        var requester = company.Add("Luz", "Junior", UserRole.Employee);
        requester.AssignSupervisor(supervisor.Id);

        await RequestAsync(company, requester);

        company.Sent.Should().ContainSingle().Which.To.Should().Equal("marcella@co.com");
    }

    [Fact]
    public async Task LeaveRequest_GoesToTheEmployers_WhenTheSupervisorDoesntApproveLeave() {
        var company = new FakeCompany();
        company.Add("Boss", "Owner", UserRole.Employer);
        var supervisor = company.Add("Marcella", "Lead", UserRole.Employee);
        var requester = company.Add("Luz", "Junior", UserRole.Employee);
        requester.AssignSupervisor(supervisor.Id, approvesLeave: false);

        await RequestAsync(company, requester);

        var email = company.Sent.Should().ContainSingle().Subject;
        email.To.Should().Equal("boss@co.com");
        email.TextBody.Should().NotContain("copy");
    }

    [Fact]
    public async Task LeaveRequest_CopiesTheEmployers_InTheirOwnLanguage_WhenTheCompanyAsks() {
        var company = new FakeCompany();
        company.Tenant.SetCopyEmployersOnLeaveRequests(true);
        company.Add("Boss", "Owner", UserRole.Employer, "nl");
        company.Add("Partner", "Two", UserRole.Employer, "fr");
        var supervisor = company.Add("Marcella", "Lead", UserRole.Employee);
        var requester = company.Add("Luz", "Junior", UserRole.Employee);
        requester.AssignSupervisor(supervisor.Id);

        await RequestAsync(company, requester);

        company.Sent.Should().HaveCount(3);
        var toSupervisor = company.Sent.Single(e => e.To.Contains("marcella@co.com"));
        toSupervisor.To.Should().ContainSingle();
        toSupervisor.TextBody.Should().NotContain("copy");
        company.Sent.Single(e => e.To.SequenceEqual(new[] { "boss@co.com" })).HtmlBody.Should()
            .Contain("Je krijgt een kopie: Marcella Lead behandelt het verlof van Luz").And.Contain(">Goedkeuren</a>");
        company.Sent.Single(e => e.To.SequenceEqual(new[] { "partner@co.com" })).TextBody.Should()
            .Contain("Vous recevez une copie");
    }

    [Fact]
    public async Task LeaveRequestEmail_HasTheLayout_WithTheDetailsInABox() {
        var company = new FakeCompany();
        var supervisor = company.Add("Marcella", "Lead", UserRole.Employee, "nl");
        var requester = company.Add("Luz", "Junior", UserRole.Employee);
        requester.AssignSupervisor(supervisor.Id);

        await RequestAsync(company, requester);

        company.Sent.Single().HtmlBody.Should()
            .StartWith("<!DOCTYPE html>")
            .And.Contain(">Shiftlog</a>") // the bar with the app's name links to the app
            .And.Contain(">Nieuwe verlofaanvraag</h1>")
            .And.Contain(">Soort</td>").And.Contain(">Verlof</td>")
            .And.Contain(">Uren</td>").And.Contain(">8,25</td>")
            .And.Contain(">Opmerking</td>").And.Contain(">Dentist</td>")
            .And.Contain("Verstuurd door Shiftlog.");
    }

    [Fact]
    public void EmailHtml_EncodesEveryText() {
        var html = TimeTracker.Application.Common.Email.EmailHtml.Document(
            new TestBranding("<b>Shift</b>"), "<script>", TimeTracker.Application.Common.Email.EmailHtml.Details("#000000", ("<i>", "<img>")));

        html.Should().NotContain("<script>").And.NotContain("<b>").And.NotContain("<img>").And.NotContain("<i>");
    }

    [Fact]
    public async Task LeaveEmails_GoOutInEachRecipientsLanguage() {
        var company = new FakeCompany();
        var supervisor = company.Add("Sam", "Lead", UserRole.Employee, "nl");
        var requester = company.Add("Eva", "Peeters", UserRole.Employee, "fr");
        requester.AssignSupervisor(supervisor.Id);

        var paidLeave = PaidLeave();
        var request = LeaveRequest.Create(requester.Id, paidLeave.Id, new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4), 3.75m, null, DayPart.Morning);
        var notifications = company.Notifications(new TestBranding());

        await notifications.RequestSubmittedAsync(request, requester, paidLeave, CancellationToken.None);
        request.Approve(supervisor.Id, null, 3.75m, DateTime.UtcNow);
        await notifications.RequestReviewedAsync(request, requester, supervisor, paidLeave, CancellationToken.None);

        var sent = company.Sent;
        sent.Should().HaveCount(2);
        sent[0].To.Should().Equal("sam@co.com");
        sent[0].Subject.Should().StartWith("Verlofaanvraag van Eva Peeters").And.Contain("voormiddag");
        sent[0].TextBody.Should().Contain("Verlof").And.Contain("3,75 uur");
        sent[1].To.Should().Equal("eva@co.com");
        sent[1].Subject.Should().StartWith("Votre demande de congé a été approuvée");
        sent[1].TextBody.Should().StartWith("Bonjour Eva,").And.Contain("Congé payé").And.Contain("matin");
        sent[1].HtmlBody.Should().Contain(">Votre congé a été approuvé</h1>");
    }
}
