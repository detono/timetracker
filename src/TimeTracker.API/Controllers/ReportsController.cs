using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.Reports.Dtos;
using TimeTracker.Application.Reports.Queries.GetHoursBalance;
using TimeTracker.Application.Reports.Queries.GetHoursReport;
using TimeTracker.Application.Reports.Queries.GetProjectHours;
using TimeTracker.Application.Reports.Queries.GetTimeEntriesExport;

namespace TimeTracker.API.Controllers;

[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class ReportsController : ApiControllerBase
{
    /// <summary>Aggregated hours extract, grouped by day/week/month, scoped to the caller's authority.</summary>
    [HttpGet("hours")]
    public async Task<ActionResult<HoursReportDto>> GetHoursReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportGrouping grouping = ReportGrouping.Week,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetHoursReportQuery(from, to, grouping, userId, projectId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Same data as <see cref="GetHoursReport"/> but returned as a downloadable CSV extract.</summary>
    [HttpGet("hours/csv")]
    public async Task<IActionResult> GetHoursReportCsv(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportGrouping grouping = ReportGrouping.Week,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetHoursReportQuery(from, to, grouping, userId, projectId), cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return HandleResult(result).Result;
        }

        var csv = new StringBuilder();
        csv.AppendLine("Employee,HourType,Period,PeriodStart,PeriodEnd,TotalHours,Entries");
        foreach (var line in result.Value.Lines)
        {
            csv.AppendLine(string.Join(',',
                Escape(line.UserFullName),
                Escape(line.HourTypeName),
                Escape(line.PeriodLabel),
                line.PeriodStart.ToString("yyyy-MM-dd"),
                line.PeriodEnd.ToString("yyyy-MM-dd"),
                line.TotalHours.ToString(CultureInfo.InvariantCulture),
                line.EntryCount));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"hours-report-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    /// <summary>Worked vs. expected hours per person per period, for the people the caller may see.</summary>
    [HttpGet("hours/balance")]
    public async Task<ActionResult<IReadOnlyList<HoursBalanceLineDto>>> GetHoursBalance(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportGrouping grouping = ReportGrouping.Week,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetHoursBalanceQuery(from, to, grouping, userId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Same as <see cref="GetHoursBalance"/>, as a CSV file. Difference is worked minus expected.</summary>
    [HttpGet("hours/balance/csv")]
    public async Task<IActionResult> GetHoursBalanceCsv(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportGrouping grouping = ReportGrouping.Week,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetHoursBalanceQuery(from, to, grouping, userId), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return HandleResult(result).Result!;
        }

        var csv = new StringBuilder();
        csv.AppendLine("Employee,Period,PeriodStart,PeriodEnd,WorkedHours,ExpectedHours,DifferenceHours");
        foreach (var line in result.Value)
        {
            csv.AppendLine(string.Join(',',
                Escape(line.UserFullName),
                Escape(line.PeriodLabel),
                line.PeriodStart.ToString("yyyy-MM-dd"),
                line.PeriodEnd.ToString("yyyy-MM-dd"),
                line.WorkedHours.ToString(CultureInfo.InvariantCulture),
                line.ExpectedHours.ToString(CultureInfo.InvariantCulture),
                line.DifferenceHours.ToString(CultureInfo.InvariantCulture)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"worked-vs-expected-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Every entry in the range as a CSV file, one row each: date, start, end, break, hours,
    /// hour type, project, location, notes, and when it was recorded and last changed (UTC).
    /// </summary>
    [HttpGet("entries/csv")]
    public async Task<IActionResult> GetTimeEntriesCsv(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetTimeEntriesExportQuery(from, to, userId, projectId), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return HandleResult(result).Result!;
        }

        var csv = new StringBuilder();
        csv.AppendLine("Employee,Date,Start,End,BreakMinutes,Hours,HourType,Project,Location,Notes,RecordedAtUtc,LastChangedAtUtc");
        foreach (var line in result.Value)
        {
            csv.AppendLine(string.Join(',',
                Escape(line.UserFullName),
                line.WorkDate.ToString("yyyy-MM-dd"),
                line.StartTime.ToString("HH:mm"),
                line.EndTime.ToString("HH:mm"),
                line.BreakMinutes,
                line.Hours.ToString(CultureInfo.InvariantCulture),
                Escape(line.HourTypeName),
                Escape(line.ProjectName ?? ""),
                Escape(line.Location ?? ""),
                Escape(line.Notes ?? ""),
                UtcStamp(line.RecordedAtUtc),
                line.LastChangedAtUtc is { } changed ? UtcStamp(changed) : ""));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"time-entries-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    /// <summary>Hours per project in the range; hours without a project get a line without one.</summary>
    [HttpGet("projects")]
    public async Task<ActionResult<IReadOnlyList<ProjectHoursLineDto>>> GetProjectHours(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await Mediator.Send(new GetProjectHoursQuery(from, to, userId), cancellationToken));
    }

    /// <summary>Same as <see cref="GetProjectHours"/>, as a CSV file.</summary>
    [HttpGet("projects/csv")]
    public async Task<IActionResult> GetProjectHoursCsv(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetProjectHoursQuery(from, to, userId), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return HandleResult(result).Result!;
        }

        var csv = new StringBuilder();
        csv.AppendLine("Project,Client,Billable,Hours,Entries");
        foreach (var line in result.Value)
        {
            csv.AppendLine(string.Join(',',
                Escape(line.ProjectName ?? ""),
                Escape(line.ClientName ?? ""),
                line.ProjectId is null ? "" : line.IsBillable ? "yes" : "no",
                line.TotalHours.ToString(CultureInfo.InvariantCulture),
                line.EntryCount));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"hours-per-project-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    private static string UtcStamp(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    /// <summary>
    /// Makes a text safe for a CSV cell: quotes it when needed, and neutralises a leading
    /// = + - @ so a spreadsheet doesn't run a name like "=HYPERLINK(...)" as a formula.
    /// </summary>
    internal static string Escape(string value)
    {
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]))
        {
            value = "'" + value;
        }

        return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
