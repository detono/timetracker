namespace TimeTracker.Application.PublicHolidays.Dtos;

public record PublicHolidayDto(Guid Id, DateOnly Date, string Name);

/// <param name="Added">How many holidays the import added; dates already in the list are kept as they are.</param>
public record PublicHolidayImportDto(int Added, IReadOnlyList<PublicHolidayDto> Holidays);
