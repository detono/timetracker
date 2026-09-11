using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Application.Reports.Dtos;
using TimeTracker.Application.Reports.Queries.GetHoursReport;

namespace TimeTracker.API.Controllers;

[Authorize]
public class ReportsController : ApiControllerBase
{
    /// <summary>Aggregated hours extract, grouped by day/week/month, scoped to the caller's authority.</summary>
    [HttpGet("hours")]
    public async Task<ActionResult<HoursReportDto>> GetHoursReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportGrouping grouping = ReportGrouping.Week,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetHoursReportQuery(from, to, grouping, userId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Same data as <see cref="GetHoursReport"/> but returned as a downloadable CSV extract.</summary>
    [HttpGet("hours/csv")]
    public async Task<IActionResult> GetHoursReportCsv(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportGrouping grouping = ReportGrouping.Week,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetHoursReportQuery(from, to, grouping, userId), cancellationToken);

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

    private static string Escape(string value) => value.Contains(',') ? $"\"{value}\"" : value;
}
