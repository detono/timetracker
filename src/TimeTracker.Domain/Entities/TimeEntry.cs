using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A single logged block of work for a given user on a given date.
/// </summary>
public class TimeEntry : BaseEntity
{
    public Guid UserId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Total break time subtracted from the worked duration, in minutes.</summary>
    public int BreakMinutes { get; private set; }

    private TimeEntry()
    {
        // EF Core
    }

    private TimeEntry(Guid userId, DateOnly workDate, TimeOnly startTime, TimeOnly endTime, int breakMinutes, string? notes)
    {
        UserId = userId;
        WorkDate = workDate;
        SetTimes(startTime, endTime, breakMinutes);
        Notes = notes;
    }

    public static TimeEntry Create(Guid userId, DateOnly workDate, TimeOnly startTime, TimeOnly endTime, int breakMinutes = 0, string? notes = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("A time entry must belong to a user.");
        }

        return new TimeEntry(userId, workDate, startTime, endTime, breakMinutes, notes);
    }

    public void SetTimes(TimeOnly startTime, TimeOnly endTime, int breakMinutes)
    {
        if (endTime <= startTime)
        {
            throw new DomainException("End time must be after start time.");
        }

        if (breakMinutes < 0)
        {
            throw new DomainException("Break minutes cannot be negative.");
        }

        var grossMinutes = (endTime - startTime).TotalMinutes;
        if (breakMinutes >= grossMinutes)
        {
            throw new DomainException("Break time cannot exceed or equal the total shift duration.");
        }

        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes;
        MarkUpdated();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        MarkUpdated();
    }

    public void Reschedule(DateOnly workDate)
    {
        WorkDate = workDate;
        MarkUpdated();
    }

    /// <summary>Net worked duration, excluding breaks.</summary>
    public TimeSpan Duration => (EndTime - StartTime) - TimeSpan.FromMinutes(BreakMinutes);
}
