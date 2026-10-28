namespace TimeTracker.Application.HourTypes.Dtos;

public record HourTypeDto(
    Guid Id,
    Dictionary<string, string> LocalizedNames,
    string ColorHex,
    bool IsActive,
    bool IsDefault,
    bool IsLeave,
    bool DeductsFromLeaveBalance
);