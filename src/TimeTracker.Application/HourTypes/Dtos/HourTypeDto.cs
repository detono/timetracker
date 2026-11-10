namespace TimeTracker.Application.HourTypes.Dtos;

public record HourTypeDto(
    Guid Id,
    Dictionary<string, string> LocalizedNames,
    string ColorHex,
    bool IsActive,
    bool IsDefault,
    bool IsLeave,
    bool DeductsFromLeaveBalance,
    string? ExternalId = null
) {
    public static HourTypeDto From(Domain.Entities.HourType t) => new(
        t.Id, t.LocalizedNames, t.ColorHex, t.IsActive, t.IsDefault, t.IsLeave, t.DeductsFromLeaveBalance, t.ExternalId);
}