using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HoursOverview.Dtos;
using TimeTracker.Domain.Interfaces;
using static TimeTracker.Application.HoursOverview.HoursOverviewCalculator;

namespace TimeTracker.Application.HoursOverview.Queries.GetMyHoursOverview;

public class GetMyHoursOverviewQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    HoursOverviewLoader loader
) : IRequestHandler<GetMyHoursOverviewQuery, Result<MyHoursOverviewDto>> {
    public async Task<Result<MyHoursOverviewDto>> Handle(GetMyHoursOverviewQuery request, CancellationToken cancellationToken) {
        var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null) {
            return Result<MyHoursOverviewDto>.Failure("Your account was not found.", ResultErrorType.NotFound);
        }

        var asOf = request.AsOf ?? clock.TodayUtc;
        var person = (await loader.LoadAsync(new[] { user }, asOf, cancellationToken))[0];
        var countFrom = CountFrom(user);
        var leaveTypes = await loader.LeaveHourTypeIdsAsync(cancellationToken);

        return Result<MyHoursOverviewDto>.Success(new MyHoursOverviewDto(
            asOf,
            user.TracksHours,
            person.Source,
            Math.Round(person.Schedule.WeeklyDuration.TotalHours, 2),
            Week(asOf, countFrom, person.Schedule, person.Entries),
            Month(asOf, countFrom, person.Schedule, person.Entries),
            Year(asOf, countFrom, person.Schedule, person.Entries),
            Days(asOf, countFrom, person.Schedule, person.Entries, leaveTypes)));
    }
}
