using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class PasswordResetCodeTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Code_WorksUntilItExpires()
    {
        var code = PasswordResetCode.Create(Guid.NewGuid(), "hash", Now);

        code.IsUsable(Now.AddMinutes(59)).Should().BeTrue();
        code.IsUsable(Now.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void Code_StopsWorkingAfterTooManyWrongGuesses()
    {
        var code = PasswordResetCode.Create(Guid.NewGuid(), "hash", Now);

        for (var i = 0; i < PasswordResetCode.MaxFailedAttempts - 1; i++) code.RecordFailedAttempt();
        code.IsUsable(Now).Should().BeTrue();

        code.RecordFailedAttempt();
        code.IsUsable(Now).Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutHash_Throws()
    {
        var act = () => PasswordResetCode.Create(Guid.NewGuid(), " ", Now);

        act.Should().Throw<DomainException>();
    }
}
