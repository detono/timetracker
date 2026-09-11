using MediatR;
using TimeTracker.Application.Common.Models;

namespace TimeTracker.Application.HourTypes.Commands.ActivateHourType;

public record ActivateHourTypeCommand(Guid Id) : IRequest<Result>;
