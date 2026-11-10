using System.Text.RegularExpressions;

namespace TimeTracker.API.Services;

/// <summary>
/// The installation's name and colours, from the same <c>APP_TITLE</c> / <c>COLOR_*</c> values
/// the web frontend uses (see docker-compose.yml). Published on <c>GET /api/info</c> so the
/// mobile app can show the right branding before anyone signs in.
/// </summary>
public partial class BrandingSettings {
    public const string SectionName = "Branding";

    public string? Title { get; set; }
    public string? ColorPrimary { get; set; }
    public string? ColorOnPrimary { get; set; }
    public string? ColorSecondary { get; set; }
    public string? ColorOnSecondary { get; set; }

    /// <summary>Where people open the web app (<c>APP_URL</c>), for links in emails.</summary>
    public string? AppUrl { get; set; }

    /// <summary>
    /// The settings as clients see them: a blank title or a value that isn't a <c>#RGB</c> /
    /// <c>#RRGGBB</c> colour becomes null, so clients fall back to their own defaults.
    /// </summary>
    public BrandingDto ToDto() => new(
        string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
        Color(ColorPrimary),
        Color(ColorOnPrimary),
        Color(ColorSecondary),
        Color(ColorOnSecondary));

    /// <summary>An absolute http(s) address without a trailing slash, or null when it's blank or not one.</summary>
    public string? NormalizedAppUrl() {
        var trimmed = AppUrl?.Trim();
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
               && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
            ? uri.GetLeftPart(UriPartial.Path).TrimEnd('/')
            : null;
    }

    internal static string? Color(string? value) {
        var trimmed = value?.Trim();
        return trimmed is not null && HexColor().IsMatch(trimmed) ? trimmed.ToLowerInvariant() : null;
    }

    [GeneratedRegex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexColor();
}

public record BrandingDto(
    string? Title,
    string? ColorPrimary,
    string? ColorOnPrimary,
    string? ColorSecondary,
    string? ColorOnSecondary);
