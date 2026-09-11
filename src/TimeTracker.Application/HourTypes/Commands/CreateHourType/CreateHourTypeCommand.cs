using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;

namespace TimeTracker.Application.HourTypes.Commands.CreateHourType;

/// <summary>Employer-only: defines a new category hours can be logged under.</summary>
public record CreateHourTypeCommand(string Name, string ColorHex) : IRequest<Result<HourTypeDto>>;
