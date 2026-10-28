using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Tenants;

/// <summary>The starter hour types every new tenant gets; its employers can rename, recolor or retire them.</summary>
public static class DefaultHourTypes
{
    public static IReadOnlyList<HourType> CreateFor(Guid tenantId)
    {
        var types = new[]
        {
            HourType.Create(
                new Dictionary<string, string> { ["en"] = "Work", ["nl"] = "Werk", ["fr"] = "Travail" },
                "#932e4a", isDefault: true),
            HourType.Create(
                new Dictionary<string, string> { ["en"] = "Sick leave", ["nl"] = "Ziekte", ["fr"] = "Congé maladie" },
                "#c0392b", isDefault: false),
            HourType.Create(
                new Dictionary<string, string> { ["en"] = "Paid time off", ["nl"] = "Verlof", ["fr"] = "Congé payé" },
                "#2a7ab0", isDefault: false)
        };

        types[1].SetLeaveOptions(isLeave: true, deductsFromBalance: false);
        types[2].SetLeaveOptions(isLeave: true, deductsFromBalance: true);

        foreach (var type in types)
        {
            type.AssignToTenant(tenantId);
        }

        return types;
    }
}
