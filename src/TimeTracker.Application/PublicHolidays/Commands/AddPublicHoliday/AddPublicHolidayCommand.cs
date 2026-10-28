using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.PublicHolidays.Dtos;

namespace TimeTracker.Application.PublicHolidays.Commands.AddPublicHoliday;

/// <summary>Employer-only: adds a holiday or company day off by hand.</summary>
public record AddPublicHolidayCommand(DateOnly Date, string Name) : IRequest<Result<PublicHolidayDto>>;
