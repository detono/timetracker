namespace TimeTracker.Application.Auth.Dtos;

/// <param name="TenantName">The user's organisation; null for platform admins.</param>
/// <param name="Language">The language the user picked; null if they haven't yet.</param>
public record AuthResultDto(string Token, Guid UserId, string FullName, string Role, string? TenantName, string? Language = null);
