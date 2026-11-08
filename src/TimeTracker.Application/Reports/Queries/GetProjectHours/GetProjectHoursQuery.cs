using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Reports.Queries.GetProjectHours;

/// <summary>
/// Hours per project in the date range, for the people the caller may see (an employer sees
/// everyone, others themself and their team). Hours without a project get their own line.
/// </summary>
public record GetProjectHoursQuery(DateOnly From, DateOnly To, Guid? UserId = null)
    : IRequest<Result<IReadOnlyList<ProjectHoursLineDto>>>;

/// <param name="ProjectId">Null for the hours that weren't logged on a project.</param>
public record ProjectHoursLineDto(
    Guid? ProjectId,
    string? ProjectName,
    string? ClientName,
    bool IsBillable,
    double TotalHours,
    int EntryCount);
