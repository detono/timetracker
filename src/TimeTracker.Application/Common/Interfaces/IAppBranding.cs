namespace TimeTracker.Application.Common.Interfaces;

/// <summary>
/// The installation's name, colours and web address (<c>APP_TITLE</c>, <c>COLOR_*</c> and
/// <c>APP_URL</c>), so emails look like the site they come from.
/// </summary>
public interface IAppBranding {
    /// <summary>The app's name; never blank.</summary>
    string Title { get; }

    /// <summary>A <c>#RGB</c> or <c>#RRGGBB</c> colour.</summary>
    string ColorPrimary { get; }

    string ColorOnPrimary { get; }
    string ColorSecondary { get; }
    string ColorOnSecondary { get; }

    /// <summary>
    /// Where people open the web app, like <c>https://hours.example.com</c> (no trailing
    /// slash), or null when it isn't set; emails then have no links.
    /// </summary>
    string? AppUrl { get; }
}
