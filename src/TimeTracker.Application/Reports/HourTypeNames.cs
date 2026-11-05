using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Reports;

/// <summary>An hour type's name in the given language, else English, else any name it has.</summary>
public static class HourTypeNames {
    public static string For(HourType? type, string language) =>
        type is null ? "Unknown"
        : type.LocalizedNames.TryGetValue(language, out var name) ? name
        : type.LocalizedNames.TryGetValue("en", out var en) ? en
        : type.LocalizedNames.Values.FirstOrDefault() ?? "Unknown";
}
