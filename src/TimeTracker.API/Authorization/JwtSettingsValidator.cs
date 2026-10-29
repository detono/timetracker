using Microsoft.Extensions.Options;
using TimeTracker.Infrastructure.Identity;

namespace TimeTracker.API.Authorization;

/// <summary>
/// Refuses to start with a signing secret anyone could know: missing, short, or one of the
/// placeholders from the repository. With such a secret anyone could sign their own tokens.
/// </summary>
public class JwtSettingsValidator(IHostEnvironment environment) : IValidateOptions<JwtSettings> {
    public const int MinimumSecretLength = 32;

    /// <summary>Only accepted when running in Development (appsettings.Development.json).</summary>
    public const string DevelopmentSecret = "development-only-secret-never-use-this-in-production";

    private static readonly string[] Placeholders = {
        "REPLACE_WITH_A_LONG_RANDOM_SECRET_IN_PRODUCTION_ENV_VARS",
        "change_me_to_a_long_random_string"
    };

    public ValidateOptionsResult Validate(string? name, JwtSettings options) {
        var secret = options.Secret?.Trim() ?? "";
        var problem =
            secret.Length < MinimumSecretLength ? $"shorter than {MinimumSecretLength} characters"
            : Placeholders.Contains(secret, StringComparer.OrdinalIgnoreCase) ? "still the placeholder from the example configuration"
            : secret == DevelopmentSecret && !environment.IsDevelopment() ? "the development secret, which is public"
            : null;

        return problem is null
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"The JWT signing secret (Jwt__Secret, JWT_SECRET in .env) is {problem}. " +
                "Set a random value of at least 32 characters, e.g. from: openssl rand -base64 48");
    }
}
