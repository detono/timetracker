using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.PublicHolidays.Dtos;

namespace TimeTracker.Application.PublicHolidays.Commands.ImportPublicHolidays;

/// <summary>
/// Employer-only: adds the official public holidays of the company's country (and region)
/// for a year. Dates already in the list are left alone, so it's safe to run again.
/// </summary>
public record ImportPublicHolidaysCommand(int Year) : IRequest<Result<PublicHolidayImportDto>>;
