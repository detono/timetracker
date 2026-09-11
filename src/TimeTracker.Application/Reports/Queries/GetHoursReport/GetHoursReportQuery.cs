using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.Reports.Dtos;

namespace TimeTracker.Application.Reports.Queries.GetHoursReport;

/// <summary>
/// Produces an aggregated extract of worked hours per employee, grouped by day, week or
/// month, for the given date range. Available to Employers (all employees) and to Employees
/// with supervisory authority (their supervisees + themselves).
/// </summary>
public record GetHoursReportQuery(
    DateOnly From,
    DateOnly To,
    ReportGrouping Grouping,
    Guid? UserId = null) : IRequest<Result<HoursReportDto>>;
