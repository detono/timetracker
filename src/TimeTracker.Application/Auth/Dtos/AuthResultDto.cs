namespace TimeTracker.Application.Auth.Dtos;

/// <param name="TenantName">The user's organisation; null for platform admins.</param>
public record AuthResultDto(string Token, Guid UserId, string FullName, string Role, string? TenantName);
