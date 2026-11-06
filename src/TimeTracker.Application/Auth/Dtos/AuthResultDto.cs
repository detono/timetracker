namespace TimeTracker.Application.Auth.Dtos;

/// <param name="TenantName">The user's organisation; null for platform admins.</param>
/// <param name="Language">The language the user picked; null if they haven't yet.</param>
/// <param name="MustChangePassword">
/// True when the password was set by someone else. Until it's changed, the API only allows
/// changing the password.
/// </param>
/// <param name="RefreshToken">
/// Only when the client asked for one (the mobile app): exchange it at <c>POST /api/auth/refresh</c>
/// for a new token without the password. Works once; each exchange returns a new one.
/// </param>
public record AuthResultDto(
    string Token, Guid UserId, string FullName, string Role, string? TenantName, string? Language = null,
    bool MustChangePassword = false, string? RefreshToken = null);
