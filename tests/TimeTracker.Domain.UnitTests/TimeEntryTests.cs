using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Exceptions;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class TimeEntryTests
{
    [Fact]
    public void Create_WithValidTimes_ComputesDuration()
    {
        var entry = TimeEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 1, 5),
            new TimeOnly(9, 0),
            new TimeOnly(17, 30),
            breakMinutes: 30);

        entry.Duration.Should().Be(TimeSpan.FromHours(8));
    }

    [Fact]
    public void Create_WithEndBeforeStart_Throws()
    {
        var act = () => TimeEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(9, 0));

        act.Should().Throw<DomainException>().WithMessage("*after*");
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var act = () => TimeEntry.Create(
            Guid.Empty, Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithEmptyHourTypeId_Throws()
    {
        var act = () => TimeEntry.Create(
            Guid.NewGuid(), Guid.Empty, new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetTimes_WithBreakExceedingDuration_Throws()
    {
        var entry = TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(10, 0));

        var act = () => entry.SetTimes(new TimeOnly(9, 0), new TimeOnly(10, 0), breakMinutes: 60);

        act.Should().Throw<DomainException>().WithMessage("*Break*");
    }

    [Fact]
    public void SetTimes_WithNegativeBreak_Throws()
    {
        var entry = TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0));

        var act = () => entry.SetTimes(new TimeOnly(9, 0), new TimeOnly(17, 0), breakMinutes: -5);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateNotes_SetsNotes()
    {
        var entry = TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0));

        entry.UpdateNotes("Worked on reports");

        entry.Notes.Should().Be("Worked on reports");
    }

    [Fact]
    public void SetHourType_WithEmptyId_Throws()
    {
        var entry = TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0));

        var act = () => entry.SetHourType(Guid.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetLocation_WithAddressAndCoordinates_StoresTrimmedValues()
    {
        var entry = TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0));

        entry.SetLocation("  Grote Markt 1, 1000 Brussel  ", 50.8467, 4.3525);

        entry.Location.Should().Be("Grote Markt 1, 1000 Brussel");
        entry.Latitude.Should().Be(50.8467);
        entry.Longitude.Should().Be(4.3525);
    }

    [Fact]
    public void SetLocation_WithBlankText_ClearsLocation()
    {
        var entry = TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0),
            location: "Office");

        entry.SetLocation("   ", null, null);

        entry.Location.Should().BeNull();
    }

    [Theory]
    [InlineData(50.0, null)]
    [InlineData(91.0, 4.0)]
    [InlineData(50.0, 181.0)]
    public void SetLocation_WithInvalidCoordinates_Throws(double? latitude, double? longitude)
    {
        var entry = TimeEntry.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 5), new TimeOnly(9, 0), new TimeOnly(17, 0));

        var act = () => entry.SetLocation("Somewhere", latitude, longitude);

        act.Should().Throw<DomainException>();
    }
}
