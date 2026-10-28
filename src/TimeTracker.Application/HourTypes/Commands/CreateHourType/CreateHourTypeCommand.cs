using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.HourTypes.Dtos;

namespace TimeTracker.Application.HourTypes.Commands.CreateHourType;

/// <summary>Employer-only: defines a new category hours can be logged under.</summary>
public record CreateHourTypeCommand(
    Dictionary<string, string> LocalizedNames, 
    string ColorHex,
    bool IsDefault,
    bool IsLeave = false,
    bool DeductsFromLeaveBalance = false
) : IRequest<Result<HourTypeDto>>;
