using TimeTracker.Domain.Exceptions;

namespace TimeTracker.Domain.Entities;

/// <summary>
/// An id something has in another system (payroll, HR), sent along in webhooks so that
/// system knows who or what an event is about.
/// </summary>
public static class ExternalIds {
    public const int MaxLength = 100;

    /// <summary>Trimmed, or null when blank.</summary>
    public static string? Normalize(string? externalId) {
        var trimmed = externalId?.Trim();
        if (string.IsNullOrEmpty(trimmed)) {
            return null;
        }

        if (trimmed.Length > MaxLength) {
            throw new DomainException($"An external ID can be at most {MaxLength} characters.");
        }

        return trimmed;
    }
}
