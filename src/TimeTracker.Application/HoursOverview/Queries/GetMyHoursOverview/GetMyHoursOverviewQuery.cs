using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HoursOverview.Dtos;

namespace TimeTracker.Application.HoursOverview.Queries.GetMyHoursOverview;

/// <summary>The caller's worked versus scheduled hours, as of <see cref="AsOf"/> (default: today).</summary>
public record GetMyHoursOverviewQuery(DateOnly? AsOf) : IRequest<Result<MyHoursOverviewDto>>;
