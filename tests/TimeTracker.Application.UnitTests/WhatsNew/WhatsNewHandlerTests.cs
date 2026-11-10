using FluentAssertions;
using Moq;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.WhatsNew;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using Xunit;

namespace TimeTracker.Application.UnitTests.WhatsNew;

public class WhatsNewHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly User _user = User.Create("Jonathan", "Peeters", "jonathan@co.com", "hash", UserRole.Employee, Guid.NewGuid());

    public WhatsNewHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Users).Returns(_users.Object);
        _users.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
    }

    // Releases dated around the moment the test user's account was created.
    private ReleaseNotes Notes(int newestDaysFromAccount)
    {
        var account = DateOnly.FromDateTime(_user.CreatedAtUtc);
        return ReleaseNotes.Parse($"""
            ## [1.3.0] - {account.AddDays(newestDaysFromAccount):yyyy-MM-dd}

            ### Added

            - Newest.

            ## [1.2.0] - {account.AddDays(-10):yyyy-MM-dd}

            ### Fixed

            - Middle.

            ## [1.1.0] - {account.AddDays(-20):yyyy-MM-dd}

            ### Fixed

            - Oldest.
            """);
    }

    private async Task<IReadOnlyList<string>> UnseenAsync(ReleaseNotes notes)
    {
        var handler = new GetWhatsNewQueryHandler(_unitOfWork.Object, new TestCurrentUserService(_user.Id, UserRole.Employee), notes);
        var result = await handler.Handle(new GetWhatsNewQuery(), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        return result.Value!.Releases.Select(r => r.Version).ToList();
    }

    private Task<Result> MarkSeenAsync(ReleaseNotes notes, string version) =>
        new MarkWhatsNewSeenCommandHandler(_unitOfWork.Object, new TestCurrentUserService(_user.Id, UserRole.Employee), notes)
            .Handle(new MarkWhatsNewSeenCommand(version), CancellationToken.None);

    [Fact]
    public async Task NeverSeen_AccountOlderThanTheNewestRelease_GetsOnlyTheNewest()
    {
        (await UnseenAsync(Notes(newestDaysFromAccount: 5))).Should().Equal("1.3.0");
    }

    [Fact]
    public async Task NeverSeen_AccountFromTheNewestReleaseOrLater_GetsNothing()
    {
        (await UnseenAsync(Notes(newestDaysFromAccount: 0))).Should().BeEmpty();
        (await UnseenAsync(Notes(newestDaysFromAccount: -3))).Should().BeEmpty();
    }

    [Fact]
    public async Task SeenBefore_GetsEveryNewerRelease_NewestFirst()
    {
        _user.MarkVersionSeen("1.1.0");

        (await UnseenAsync(Notes(newestDaysFromAccount: 5))).Should().Equal("1.3.0", "1.2.0");
    }

    [Fact]
    public async Task SeenTheNewest_GetsNothing()
    {
        _user.MarkVersionSeen("1.3");

        (await UnseenAsync(Notes(newestDaysFromAccount: 5))).Should().BeEmpty();
    }

    [Fact]
    public async Task MarkingSeen_StopsTheNotesShowing()
    {
        var notes = Notes(newestDaysFromAccount: 5);

        (await MarkSeenAsync(notes, "1.3.0")).Succeeded.Should().BeTrue();

        _user.LastSeenVersion.Should().Be("1.3.0");
        (await UnseenAsync(notes)).Should().BeEmpty();
    }

    [Fact]
    public async Task MarkingAnOlderVersionSeen_DoesNotBringNewerNotesBack()
    {
        var notes = Notes(newestDaysFromAccount: 5);
        _user.MarkVersionSeen("1.3.0");

        (await MarkSeenAsync(notes, "1.1.0")).Succeeded.Should().BeTrue();

        _user.LastSeenVersion.Should().Be("1.3.0");
    }

    [Fact]
    public async Task MarkingAnUnknownVersionSeen_IsNotFound()
    {
        var result = await MarkSeenAsync(Notes(newestDaysFromAccount: 5), "9.9.9");

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        _user.LastSeenVersion.Should().BeNull();
    }
}
