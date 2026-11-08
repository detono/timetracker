using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.Reports.Queries.GetTimeEntriesExport;

/// <summary>
/// Every logged entry in the date range, one line each, for the people the caller may see
/// (an employer sees everyone, others themself and their team). Meant for a working-time
/// record: when someone worked, not only how much. With a <see cref="ProjectId"/>, only
/// the entries on that project.
/// </summary>
public record GetTimeEntriesExportQuery(
    DateOnly From,
    DateOnly To,
    Guid? UserId = null,
    Guid? ProjectId = null) : IRequest<Result<IReadOnlyList<TimeEntryExportLineDto>>>;

public record TimeEntryExportLineDto(
    Guid UserId,
    string UserFullName,
    DateOnly WorkDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    double Hours,
    string HourTypeName,
    string? ProjectName,
    string? Location,
    string? Notes,
    DateTime RecordedAtUtc,
    DateTime? LastChangedAtUtc);
