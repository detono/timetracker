using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.PublicHolidays.Dtos;

namespace TimeTracker.Application.PublicHolidays.Queries.GetPublicHolidays;

/// <summary>The company's public holidays and days off in a year. Visible to everyone in the company.</summary>
public record GetPublicHolidaysQuery(int Year) : IRequest<Result<IReadOnlyList<PublicHolidayDto>>>;
