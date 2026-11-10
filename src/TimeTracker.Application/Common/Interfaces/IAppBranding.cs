namespace TimeTracker.Application.Common.Interfaces;

/// <summary>The installation's name (<c>APP_TITLE</c>), so emails call the app what the site does.</summary>
public interface IAppBranding {
    /// <summary>The app's name; never blank.</summary>
    string Title { get; }
}
