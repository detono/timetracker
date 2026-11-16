namespace TimeTracker.Application.Common.Security;

public static class SessionClaimTypes
{
    /// <summary>The user's <c>SessionVersion</c> when the token was issued. Missing on older tokens, which count as 0.</summary>
    public const string SessionVersion = "session_version";

    /// <summary>Present (as "true") while the user still has to replace a temporary password.</summary>
    public const string MustChangePassword = "must_change_password";

    /// <summary>
    /// On a support sign-in: the platform admin who is signed in as this user. The token is
    /// otherwise the user's own, so everything they can do works the same.
    /// </summary>
    public const string ImpersonatorId = "impersonator_id";

    /// <summary>The platform admin's name, for the activity log.</summary>
    public const string ImpersonatorName = "impersonator_name";
}
