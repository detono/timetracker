using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.PublicHolidays.Commands.DeletePublicHoliday;

/// <summary>Employer-only: removes a holiday, e.g. one the company works on anyway.</summary>
public record DeletePublicHolidayCommand(Guid Id) : IRequest<Result>;
