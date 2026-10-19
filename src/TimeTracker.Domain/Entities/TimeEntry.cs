using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A single logged block of time for a given user on a given date, categorized by an
/// employer-managed <see cref="HourType"/> (Work, Sick Leave, PTO, ADV, ...).
/// </summary>
public class TimeEntry : BaseEntity {
    public Guid UserId { get; private set; }
    public Guid HourTypeId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Human-readable place where the work was done (free text or a reverse-geocoded address).</summary>
    public string? Location { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    /// <summary>Total break time subtracted from the worked duration, in minutes.</summary>
    public int BreakMinutes { get; private set; }

    private TimeEntry() {
        // EF Core
    }

    private TimeEntry(
        Guid userId, 
        Guid hourTypeId, 
        DateOnly workDate, 
        TimeOnly startTime, 
        TimeOnly endTime,
        int breakMinutes, 
        string? notes,
        Guid? projectId,
        string? location,
        double? latitude,
        double? longitude
    ) {
        UserId = userId;
        SetHourType(hourTypeId);
        WorkDate = workDate;
        SetTimes(startTime, endTime, breakMinutes);
        Notes = notes;
        ProjectId = projectId;
        SetLocation(location, latitude, longitude);
    }

    public static TimeEntry Create(
        Guid userId,
        Guid hourTypeId,
        DateOnly workDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes = 0,
        string? notes = null,
        Guid? projectId = null,
        string? location = null,
        double? latitude = null,
        double? longitude = null
    ) {
        if (userId == Guid.Empty) {
            throw new DomainException("A time entry must belong to a user.");
        }

        return new TimeEntry(
            userId, 
            hourTypeId, 
            workDate, 
            startTime, 
            endTime, 
            breakMinutes, 
            notes,
            projectId,
            location,
            latitude,
            longitude
        );
    }

    public void SetHourType(Guid hourTypeId) {
        if (hourTypeId == Guid.Empty) {
            throw new DomainException("A time entry must have an hour type.");
        }

        HourTypeId = hourTypeId;
        MarkUpdated();
    }
    
    public void SetProject(Guid? projectId) {
        ProjectId = projectId;
        MarkUpdated();
    }

    public void SetTimes(TimeOnly startTime, TimeOnly endTime, int breakMinutes) {
        if (endTime <= startTime) {
            throw new DomainException("End time must be after start time.");
        }

        if (breakMinutes < 0) {
            throw new DomainException("Break minutes cannot be negative.");
        }

        var grossMinutes = (endTime - startTime).TotalMinutes;
        if (breakMinutes >= grossMinutes) {
            throw new DomainException("Break time cannot exceed or equal the total shift duration.");
        }

        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes;
        MarkUpdated();
    }

    public void UpdateNotes(string? notes) {
        Notes = notes;
        MarkUpdated();
    }

    public void UpdateProjectId(Guid? projectId) {
        ProjectId = projectId;
        MarkUpdated();
    }

    public void SetLocation(string? location, double? latitude, double? longitude) {
        if (latitude.HasValue != longitude.HasValue) {
            throw new DomainException("Latitude and longitude must be provided together.");
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) {
            throw new DomainException("Coordinates are out of range.");
        }

        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        Latitude = latitude;
        Longitude = longitude;
        MarkUpdated();
    }

    public void Reschedule(DateOnly workDate) {
        WorkDate = workDate;
        MarkUpdated();
    }

    /// <summary>Net worked duration, excluding breaks.</summary>
    public TimeSpan Duration => (EndTime - StartTime) - TimeSpan.FromMinutes(BreakMinutes);
}