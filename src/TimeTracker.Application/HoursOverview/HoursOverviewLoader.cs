using TimeTracker.Application.Leave;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.HoursOverview;

/// <summary>Loads what the overview needs for several people in a fixed number of queries.</summary>
public class HoursOverviewLoader(IUnitOfWork unitOfWork) {
    public record PersonData(User User, WorkSchedule Schedule, WorkScheduleSource Source, IReadOnlyCollection<TimeEntry> Entries);

    /// <summary>
    /// Each person's schedule and their entries from 1 January of the reference year up to
    /// the Sunday of the reference week (so days later this week can show booked leave).
    /// </summary>
    public async Task<IReadOnlyList<PersonData>> LoadAsync(
        IReadOnlyList<User> users, DateOnly asOf, CancellationToken cancellationToken
    ) {
        if (users.Count == 0) {
            return Array.Empty<PersonData>();
        }

        var schedules = await unitOfWork.WorkSchedules.GetAllAsync(cancellationToken);
        var company = schedules.FirstOrDefault(s => s.IsCompanyDefault);
        var personal = schedules.Where(s => s.UserId is not null).ToDictionary(s => s.UserId!.Value);

        var from = new DateOnly(asOf.Year, 1, 1);
        var sunday = HoursOverviewCalculator.StartOfWeek(asOf).AddDays(6);
        var entries = (await unitOfWork.TimeEntries.GetForUsersAsync(
                users.Select(u => u.Id).ToList(), from, sunday, cancellationToken))
            .ToLookup(e => e.UserId);

        return users.Select(user => {
            var (schedule, source) = personal.TryGetValue(user.Id, out var own)
                ? (own, WorkScheduleSource.Personal)
                : company is not null
                    ? (company, WorkScheduleSource.Company)
                    : (WorkSchedule.BuiltInDefault(), WorkScheduleSource.BuiltIn);
            return new PersonData(user, schedule, source, entries[user.Id].ToList());
        }).ToList();
    }

    /// <summary>Holidays from 1 January of the reference year to the Sunday of the reference week, by date.</summary>
    public async Task<IReadOnlyDictionary<DateOnly, string>> HolidaysAsync(DateOnly asOf, CancellationToken cancellationToken) =>
        (await unitOfWork.PublicHolidays.GetBetweenAsync(
            new DateOnly(asOf.Year, 1, 1), HoursOverviewCalculator.StartOfWeek(asOf).AddDays(6), cancellationToken))
        .ToDictionary(h => h.Date, h => h.Name);

    public async Task<HashSet<Guid>> LeaveHourTypeIdsAsync(CancellationToken cancellationToken) =>
        (await unitOfWork.HourTypes.GetAllAsync(includeInactive: true, cancellationToken))
        .Where(t => t.IsLeave)
        .Select(t => t.Id)
        .ToHashSet();
}
