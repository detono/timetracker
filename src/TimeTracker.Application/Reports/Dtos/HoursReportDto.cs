namespace TimeTracker.Application.Reports.Dtos;

public record HoursReportLineDto(
    Guid UserId,
    string UserFullName,
    Guid HourTypeId,
    string HourTypeName,
    string HourTypeColor,
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    double TotalHours,
    int EntryCount);

public record HoursReportDto(
    string Grouping,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<HoursReportLineDto> Lines,
    double GrandTotalHours);
