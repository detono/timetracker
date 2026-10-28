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
    public async Task LeaveEmails_GoOutInEachRecipientsLanguage() {
        var tenantId = Guid.NewGuid();
        var supervisor = User.Create("Sam", "Lead", "sam@co.com", "hash", UserRole.Employee, tenantId);
        supervisor.SetLanguage("nl");
        var requester = User.Create("Eva", "Peeters", "eva@co.com", "hash", UserRole.Employee, tenantId);
        requester.AssignSupervisor(supervisor.Id);
        requester.SetLanguage("fr");

        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(supervisor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(supervisor);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.Users).Returns(users.Object);
        var sent = new List<EmailMessage>();
        var sender = new Mock<IEmailSender>();
        sender.Setup(s => s.SendAsync(Capture.In(sent), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendResult.Sent());

        var paidLeave = HourType.Create(new Dictionary<string, string> { ["en"] = "Paid time off", ["nl"] = "Verlof", ["fr"] = "Congé payé" }, "#2a7ab0", false);
        var request = LeaveRequest.Create(requester.Id, paidLeave.Id, new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4), 3.75m, null, DayPart.Morning);
        var notifications = new LeaveNotifications(unitOfWork.Object, sender.Object);

        await notifications.RequestSubmittedAsync(request, requester, paidLeave, CancellationToken.None);
        request.Approve(supervisor.Id, null, 3.75m, DateTime.UtcNow);
        await notifications.RequestReviewedAsync(request, requester, supervisor, paidLeave, CancellationToken.None);

        sent.Should().HaveCount(2);
        sent[0].To.Should().Equal("sam@co.com");
        sent[0].Subject.Should().StartWith("Verlofaanvraag van Eva Peeters").And.Contain("voormiddag");
        sent[0].TextBody.Should().Contain("Verlof").And.Contain("3,75 uur");
        sent[1].To.Should().Equal("eva@co.com");
        sent[1].Subject.Should().StartWith("Votre demande de congé a été approuvée");
        sent[1].TextBody.Should().StartWith("Bonjour Eva,").And.Contain("Congé payé").And.Contain("matin");
    }
}
