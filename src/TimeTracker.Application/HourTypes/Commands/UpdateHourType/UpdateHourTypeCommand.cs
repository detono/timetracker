using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;

namespace TimeTracker.Application.HourTypes.Commands.UpdateHourType;

/// <summary>Employer-only: renames and/or recolors an existing hour type.</summary>

public record UpdateHourTypeCommand(
    Guid Id,
    Dictionary<string, string> LocalizedNames,
    string ColorHex,
    bool IsDefault
) : IRequest<Result<HourTypeDto>>;

