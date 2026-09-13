using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A category of logged time - "Work", "Sick Leave", "PTO", "ADV", or whatever else an
/// Employer chooses to define. Types are never hard-deleted once in use (a
/// <see cref="TimeEntry"/> always needs a valid type to point to for reporting), so
/// retiring one is a <see cref="Deactivate"/> instead: it stops showing up as an option
/// for new entries, but historical entries that reference it are unaffected.
/// </summary>
public class HourType : BaseEntity {
    public Dictionary<string, string> LocalizedNames { get; private set; } = new();
    
    /// <summary>Hex color (e.g. "#932e4a") used to render this type consistently in the UI.</summary>
    public string ColorHex { get; private set; } = default!;

    public bool IsActive { get; private set; } = true;
    
    // 1. Renamed to IsDefault for consistency
    public bool IsDefault { get; private set; } = false;
    
    private HourType() {
        // EF Core
    }

    private HourType(Dictionary<string, string> localizedNames, string colorHex, bool isDefault) {
        SetLocalizedNames(localizedNames);
        SetColor(colorHex);
        IsActive = true;
        IsDefault = isDefault;
    }

    public static HourType Create(Dictionary<string, string> localizedNames, string colorHex, bool isDefault) {
        return new HourType(localizedNames, colorHex, isDefault);
    }

    public void Update(Dictionary<string, string> localizedNames, string colorHex, bool isDefault) {
        SetLocalizedNames(localizedNames);
        SetColor(colorHex);
        IsDefault = isDefault; 
    }

    public void SetLocalizedNames(Dictionary<string, string> localizedNames) {
        if (localizedNames == null || !localizedNames.Any()) {
            throw new DomainException("At least one localized name must be provided.");
        }

        LocalizedNames = localizedNames;
        MarkUpdated();
    }

    public void SetColor(string colorHex) {
        if (string.IsNullOrWhiteSpace(colorHex) || !colorHex.StartsWith('#') || colorHex.Length is not (4 or 7)) {
            throw new DomainException("Color must be a hex value like #932e4a.");
        }

        ColorHex = colorHex;
        MarkUpdated();
    }
    
    public void SetAsDefault() {
        IsDefault = true;
        MarkUpdated();
    }

    public void RemoveDefault() {
        IsDefault = false;
        MarkUpdated();
    }

    public void Deactivate() {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate() {
        IsActive = true;
        MarkUpdated();
    }
}