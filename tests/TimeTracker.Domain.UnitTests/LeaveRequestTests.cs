using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class LeaveRequestTests {
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static LeaveRequest NewRequest() => LeaveRequest.Create(
        Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), 37.5m, "  Holiday ");

    [Fact]
    public void Create_StartsPendingWithTrimmedComment() {
        var request = NewRequest();

        request.Status.Should().Be(LeaveRequestStatus.Pending);
        request.Comment.Should().Be("Holiday");
    }

    [Fact]
    public void Create_WithEndBeforeStart_Throws() {
        var act = () => LeaveRequest.Create(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 2), 8, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithoutWorkingHours_Throws() {
        var act = () => LeaveRequest.Create(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 7), new DateOnly(2026, 11, 8), 0, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Approve_RecordsReviewerAndHours() {
        var request = NewRequest();
        var reviewer = Guid.NewGuid();

        request.Approve(reviewer, "Enjoy", 30m, Now);

        request.Status.Should().Be(LeaveRequestStatus.Approved);
        request.ReviewedById.Should().Be(reviewer);
        request.ReviewedAtUtc.Should().Be(Now);
        request.ReviewComment.Should().Be("Enjoy");
        request.Hours.Should().Be(30m);
    }

    [Fact]
    public void Approve_WhenAlreadyRejected_Throws() {
        var request = NewRequest();
        request.Reject(Guid.NewGuid(), null, Now);

        var act = () => request.Approve(Guid.NewGuid(), null, 8, Now);

        act.Should().Throw<DomainException>().WithMessage("*approved*rejected*");
    }

    [Fact]
    public void Cancel_WorksForPendingAndApprovedButNotTwice() {
        var pending = NewRequest();
        pending.Cancel(Guid.NewGuid(), null, Now);
        pending.Status.Should().Be(LeaveRequestStatus.Cancelled);

        var approved = NewRequest();
        approved.Approve(Guid.NewGuid(), null, 8, Now);
        approved.Cancel(Guid.NewGuid(), null, Now);
        approved.Status.Should().Be(LeaveRequestStatus.Cancelled);

        var act = () => approved.Cancel(Guid.NewGuid(), null, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void HourType_OnlyLeaveTypesCanDeductFromTheBalance() {
        var type = HourType.Create(new Dictionary<string, string> { ["en"] = "Work" }, "#932e4a", true);

        type.SetLeaveOptions(isLeave: false, deductsFromBalance: true);

        type.DeductsFromLeaveBalance.Should().BeFalse();
    }
}
