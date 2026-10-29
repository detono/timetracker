using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.API.Authorization;
using TimeTracker.Application.Reports.Dtos;
using TimeTracker.Application.Reports.Queries.GetHoursBalance;
using TimeTracker.Application.Reports.Queries.GetHoursReport;

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
