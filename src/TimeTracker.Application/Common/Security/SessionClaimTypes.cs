namespace TimeTracker.Application.Common.Security;

public static class SessionClaimTypes
{
    /// <summary>The user's <c>SessionVersion</c> when the token was issued. Missing on older tokens, which count as 0.</summary>
    public const string SessionVersion = "session_version";

    /// <summary>Present (as "true") while the user still has to replace a temporary password.</summary>
    public const string MustChangePassword = "must_change_password";
}
