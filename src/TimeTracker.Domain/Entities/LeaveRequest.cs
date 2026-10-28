using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A request for time off over a range of dates, under a leave <see cref="HourType"/>.
/// Approving it books one time entry per working day (see <see cref="WorkSchedule"/>), so
/// the leave shows up in the planning and in the leave balance like any other logged hours.
/// </summary>
public class LeaveRequest : TenantOwnedEntity {
    public const int MaxRangeDays = 366;

    public Guid UserId { get; private set; }
    public Guid HourTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Comment { get; private set; }

    /// <summary>The part of the first day that's taken off; for a single day, the part of that day.</summary>
    public DayPart FirstDayPart { get; private set; }

    /// <summary>The part of the last day that's taken off. Equals <see cref="FirstDayPart"/> for a single day.</summary>
    public DayPart LastDayPart { get; private set; }

    /// <summary>The scheduled hours this leave covers. Recalculated on approval.</summary>
    public decimal Hours { get; private set; }

    public LeaveRequestStatus Status { get; private set; }
    public Guid? ReviewedById { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? ReviewComment { get; private set; }

    private LeaveRequest() {
        // EF Core
    }

    public static LeaveRequest Create(
        Guid userId, Guid hourTypeId, DateOnly startDate, DateOnly endDate, decimal hours, string? comment,
        DayPart firstDayPart = DayPart.Full, DayPart lastDayPart = DayPart.Full
    ) {
        if (userId == Guid.Empty) {
            throw new DomainException("A leave request must belong to a user.");
        }

        if (hourTypeId == Guid.Empty) {
            throw new DomainException("A leave request needs an hour type.");
        }

        if (endDate < startDate) {
            throw new DomainException("The end date can't be before the start date.");
        }

        if (endDate.DayNumber - startDate.DayNumber >= MaxRangeDays) {
            throw new DomainException("A leave request can span at most one year.");
        }

        if (startDate == endDate) {
            lastDayPart = firstDayPart;
        }

        LeavePlan.EnsureValidParts(startDate, endDate, firstDayPart, lastDayPart);

        if (hours <= 0) {
            throw new DomainException("This period has no working days in the work schedule.");
        }

        return new LeaveRequest {
            UserId = userId,
            HourTypeId = hourTypeId,
            StartDate = startDate,
            EndDate = endDate,
            Hours = hours,
            Comment = Normalize(comment),
            FirstDayPart = firstDayPart,
            LastDayPart = lastDayPart,
            Status = LeaveRequestStatus.Pending
        };
    }

    public void Approve(Guid reviewerId, string? comment, decimal hours, DateTime nowUtc) {
        EnsurePending("approved");
        Hours = hours;
        Review(LeaveRequestStatus.Approved, reviewerId, comment, nowUtc);
    }

    public void Reject(Guid reviewerId, string? comment, DateTime nowUtc) {
        EnsurePending("rejected");
        Review(LeaveRequestStatus.Rejected, reviewerId, comment, nowUtc);
    }

    /// <summary>Withdraws a pending request, or revokes an approved one.</summary>
    public void Cancel(Guid cancelledById, string? comment, DateTime nowUtc) {
        if (Status is not (LeaveRequestStatus.Pending or LeaveRequestStatus.Approved)) {
            throw new DomainException($"A {Status.ToString().ToLowerInvariant()} request can't be cancelled.");
        }

        Review(LeaveRequestStatus.Cancelled, cancelledById, comment, nowUtc);
    }

    public bool Overlaps(DateOnly start, DateOnly end) => StartDate <= end && start <= EndDate;

    private void EnsurePending(string pastTense) {
        if (Status != LeaveRequestStatus.Pending) {
            throw new DomainException($"Only a pending request can be {pastTense}; this one is {Status.ToString().ToLowerInvariant()}.");
        }
    }

    private void Review(LeaveRequestStatus status, Guid reviewerId, string? comment, DateTime nowUtc) {
        Status = status;
        ReviewedById = reviewerId;
        ReviewedAtUtc = nowUtc;
        ReviewComment = Normalize(comment);
        MarkUpdated();
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
