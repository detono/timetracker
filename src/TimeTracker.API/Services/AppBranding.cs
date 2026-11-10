using Microsoft.Extensions.Options;
using TimeTracker.Application.Common.Interfaces;

namespace TimeTracker.API.Services;

/// <summary>The branding for emails, with the frontend's defaults for anything not set.</summary>
public class AppBranding(IOptions<BrandingSettings> options) : IAppBranding {
    private readonly BrandingSettings _settings = options.Value;

    public string Title => string.IsNullOrWhiteSpace(_settings.Title) ? "TimeTracker" : _settings.Title.Trim();
    public string ColorPrimary => BrandingSettings.Color(_settings.ColorPrimary) ?? "#932e4a";
    public string ColorOnPrimary => BrandingSettings.Color(_settings.ColorOnPrimary) ?? "#ffffff";
    public string ColorSecondary => BrandingSettings.Color(_settings.ColorSecondary) ?? "#e4d1dc";
    public string ColorOnSecondary => BrandingSettings.Color(_settings.ColorOnSecondary) ?? "#000000";
    public string? AppUrl => _settings.NormalizedAppUrl();
}
