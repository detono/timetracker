using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);

    private static readonly User Employee =
        User.Create("Eva", "Employee", "eva@acme.test", "hash", UserRole.Employee, Guid.NewGuid());

    private static RefreshToken NewToken() => RefreshToken.Issue(Employee, "hash", Guid.NewGuid(), Now, Lifetime);

    [Fact]
    public void Issue_RemembersUserCompanyAndSessionVersion()
    {
        var token = NewToken();

        token.UserId.Should().Be(Employee.Id);
        token.TenantId.Should().Be(Employee.TenantId);
        token.SessionVersion.Should().Be(Employee.SessionVersion);
        token.ExpiresAtUtc.Should().Be(Now + Lifetime);
    }

    [Fact]
    public void Token_WorksUntilItExpires()
    {
        var token = NewToken();

        token.CanBeExchanged(Now + Lifetime - TimeSpan.FromSeconds(1)).Should().BeTrue();
        token.CanBeExchanged(Now + Lifetime).Should().BeFalse();
    }

    [Fact]
    public void UsedToken_StillWorksDuringGracePeriod_ThenCountsAsReplay()
    {
        var token = NewToken();
        token.MarkUsed(Now);

        token.CanBeExchanged(Now + RefreshToken.ReuseGracePeriod).Should().BeTrue();
        token.IsReplay(Now + RefreshToken.ReuseGracePeriod).Should().BeFalse();

        var later = Now + RefreshToken.ReuseGracePeriod + TimeSpan.FromSeconds(1);
        token.CanBeExchanged(later).Should().BeFalse();
        token.IsReplay(later).Should().BeTrue();
    }

    [Fact]
    public void RevokedToken_CannotBeExchanged()
    {
        var token = NewToken();
        token.Revoke(Now);

        token.CanBeExchanged(Now).Should().BeFalse();
        var act = () => token.MarkUsed(Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Issue_WithoutHashOrLifetime_Throws()
    {
        var noHash = () => RefreshToken.Issue(Employee, " ", Guid.NewGuid(), Now, Lifetime);
        var noLifetime = () => RefreshToken.Issue(Employee, "hash", Guid.NewGuid(), Now, TimeSpan.Zero);

        noHash.Should().Throw<DomainException>();
        noLifetime.Should().Throw<DomainException>();
    }
}
