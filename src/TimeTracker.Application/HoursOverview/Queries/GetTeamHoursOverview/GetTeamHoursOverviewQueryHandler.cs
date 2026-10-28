using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HoursOverview.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;
using static TimeTracker.Application.HoursOverview.HoursOverviewCalculator;

namespace TimeTracker.Application.HoursOverview.Queries.GetTeamHoursOverview;

public class GetTeamHoursOverviewQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    HoursOverviewLoader loader
) : IRequestHandler<GetTeamHoursOverviewQuery, Result<IReadOnlyList<TeamMemberHoursDto>>> {
    public async Task<Result<IReadOnlyList<TeamMemberHoursDto>>> Handle(
        GetTeamHoursOverviewQuery request, CancellationToken cancellationToken
    ) {
        IReadOnlyList<User> users = currentUser.Role == UserRole.Employer
            ? await unitOfWork.Users.GetAllAsync(cancellationToken)
            : await unitOfWork.Users.GetSupervisedByAsync(currentUser.UserId, cancellationToken);
        users = users.Where(u => u.IsActive && u.TracksHours).OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToList();

        var asOf = request.AsOf ?? clock.TodayUtc;
        var people = await loader.LoadAsync(users, asOf, cancellationToken);

        IReadOnlyList<TeamMemberHoursDto> rows = people.Select(p => {
            var countFrom = CountFrom(p.User);
            return new TeamMemberHoursDto(
                p.User.Id,
                $"{p.User.FirstName} {p.User.LastName}",
                p.Source,
                Math.Round(p.Schedule.WeeklyDuration.TotalHours, 2),
                Week(asOf, countFrom, p.Schedule, p.Entries),
                Month(asOf, countFrom, p.Schedule, p.Entries),
                Year(asOf, countFrom, p.Schedule, p.Entries));
        }).ToList();

        return Result<IReadOnlyList<TeamMemberHoursDto>>.Success(rows);
    }
}
