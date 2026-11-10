using Microsoft.Extensions.Options;
using TimeTracker.Application.Common.Interfaces;

namespace TimeTracker.API.Services;

/// <summary>The branding for emails, with the frontend's default name when none is set.</summary>
public class AppBranding(IOptions<BrandingSettings> options) : IAppBranding {
    private readonly BrandingSettings _settings = options.Value;

    public string Title => string.IsNullOrWhiteSpace(_settings.Title) ? "TimeTracker" : _settings.Title.Trim();
}
