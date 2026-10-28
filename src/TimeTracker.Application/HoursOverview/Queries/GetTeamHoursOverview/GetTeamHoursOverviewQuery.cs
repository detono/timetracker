using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HoursOverview.Dtos;

namespace TimeTracker.Application.HoursOverview.Queries.GetTeamHoursOverview;

/// <summary>
/// Worked versus scheduled hours per person: everyone active for employers, the people
/// they supervise for supervisors, and nobody for other employees. People who don't track
/// hours are left out.
/// </summary>
public record GetTeamHoursOverviewQuery(DateOnly? AsOf) : IRequest<Result<IReadOnlyList<TeamMemberHoursDto>>>;
