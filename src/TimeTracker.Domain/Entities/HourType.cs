using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// A category of logged time - "Work", "Sick Leave", "PTO", "ADV", or whatever else an
/// Employer chooses to define. Types are never hard-deleted once in use (a
/// <see cref="TimeEntry"/> always needs a valid type to point to for reporting), so
/// retiring one is a <see cref="Deactivate"/> instead: it stops showing up as an option
/// for new entries, but historical entries that reference it are unaffected.
/// </summary>
public class HourType : BaseEntity
{
    public string Name { get; private set; } = default!;

    /// <summary>Hex color (e.g. "#932e4a") used to render this type consistently in the UI.</summary>
    public string ColorHex { get; private set; } = default!;

    public bool IsActive { get; private set; } = true;

    private HourType()
    {
        // EF Core
    }

    private HourType(string name, string colorHex)
    {
        SetName(name);
        SetColor(colorHex);
    }

    public static HourType Create(string name, string colorHex) => new(name, colorHex);

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Hour type name is required.");
        }

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetColor(string colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex) || !colorHex.StartsWith('#') || colorHex.Length is not (4 or 7))
        {
            throw new DomainException("Color must be a hex value like #932e4a.");
        }

        ColorHex = colorHex;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }
}
