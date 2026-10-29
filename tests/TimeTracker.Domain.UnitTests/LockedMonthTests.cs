using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class LockedMonthTests
{
    private static readonly DateOnly Today = new(2026, 10, 29);

    [Fact]
    public void Create_CoversTheWholeMonth()
    {
        var month = LockedMonth.Create(2026, 2, Guid.NewGuid(), Today);

        month.FirstDay.Should().Be(new DateOnly(2026, 2, 1));
        month.LastDay.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void Create_AllowsTheCurrentMonth_ButNotAFutureOne()
    {
        LockedMonth.Create(2026, 10, Guid.NewGuid(), Today).Month.Should().Be(10);

        var act = () => LockedMonth.Create(2026, 11, Guid.NewGuid(), Today);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(1999, 5)]
    public void Create_RejectsInvalidMonths(int year, int month)
    {
        var act = () => LockedMonth.Create(year, month, Guid.NewGuid(), Today);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Index_OrdersMonthsAcrossYears()
    {
        LockedMonth.Index(new DateOnly(2025, 12, 31)).Should().BeLessThan(LockedMonth.Index(new DateOnly(2026, 1, 1)));
        LockedMonth.Index(new DateOnly(2026, 1, 1)).Should().Be(LockedMonth.Index(new DateOnly(2026, 1, 31)));
    }
}
