using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.HourTypes.Commands.DeactivateHourType;

/// <summary>Employer-only: retires an hour type. Existing entries keep it; it stops appearing as an option for new ones.</summary>
public record DeactivateHourTypeCommand(Guid Id) : IRequest<Result>;
