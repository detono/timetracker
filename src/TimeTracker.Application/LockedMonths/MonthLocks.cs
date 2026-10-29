using System.Globalization;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.LockedMonths;

/// <summary>
/// The one check every change to hours goes through: logging, editing or deleting a time
/// entry, and booking or removing leave. Locked months apply to everyone, employers included;
/// an employer unlocks the month first to fix something.
/// </summary>
public class MonthLocks(IUnitOfWork unitOfWork) {
    /// <summary>
    /// Null when no day from <paramref name="start"/> to <paramref name="end"/> is in a locked
    /// month; otherwise a message naming the first locked month.
    /// </summary>
    public async Task<string?> FindLockedAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken) {
        if (end < start) {
            (start, end) = (end, start);
        }

        var locked = await unitOfWork.LockedMonths.GetBetweenAsync(start, end, cancellationToken);
        return locked.Count == 0 ? null : MessageFor(locked[0]);
    }

    public Task<string?> FindLockedAsync(DateOnly date, CancellationToken cancellationToken) =>
        FindLockedAsync(date, date, cancellationToken);

    /// <summary>Which of the given months are locked, as <see cref="LockedMonth.Index"/> values.</summary>
    public async Task<IReadOnlySet<int>> LockedIndexesAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken) =>
        (await unitOfWork.LockedMonths.GetBetweenAsync(start, end, cancellationToken))
        .Select(m => LockedMonth.Index(m.FirstDay))
        .ToHashSet();

    public static string MessageFor(LockedMonth month) =>
        $"{month.FirstDay.ToString("MMMM yyyy", CultureInfo.InvariantCulture)} is locked. " +
        "An employer has to unlock it before hours in it can change.";
}
