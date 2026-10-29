using TimeTracker.Application.Common.Security;

namespace TimeTracker.API.Middleware;

/// <summary>
/// While someone still has a temporary password (set by an employer or platform admin), the
/// only thing they can do is choose their own. Enforced here so it doesn't depend on the UI.
/// </summary>
public class PasswordChangeRequiredMiddleware(RequestDelegate next) {
    public const string ErrorCode = "passwordChangeRequired";

    private static readonly string[] AllowedPaths = {
        "/api/auth/change-password",
        "/api/auth/language"
    };

    public async Task InvokeAsync(HttpContext context) {
        if (context.User.HasClaim(SessionClaimTypes.MustChangePassword, "true")
            && !AllowedPaths.Any(p => context.Request.Path.Equals(p, StringComparison.OrdinalIgnoreCase))) {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new {
                error = "Choose a new password first.",
                code = ErrorCode
            });
            return;
        }

        await next(context);
    }
}
