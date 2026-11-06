namespace TimeTracker.Infrastructure.Identity;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiryMinutes { get; set; } = 480;

    /// <summary>
    /// How many days a refresh token (mobile app) stays valid without being used. Each use
    /// gives a new one, so someone who opens the app at least this often stays signed in.
    /// </summary>
    public int RefreshTokenDays { get; set; } = 14;
}
