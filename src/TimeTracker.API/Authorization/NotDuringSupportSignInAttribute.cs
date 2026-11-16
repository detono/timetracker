using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TimeTracker.Application.Common.Security;

namespace TimeTracker.API.Authorization;

/// <summary>
/// For the user's own account settings (password, language): a platform admin signed in as
/// them for support can't change these.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class NotDuringSupportSignInAttribute : Attribute, IAuthorizationFilter {
    public const string ErrorCode = "notDuringSupportSignIn";

    public void OnAuthorization(AuthorizationFilterContext context) {
        if (context.HttpContext.User.HasClaim(c => c.Type == SessionClaimTypes.ImpersonatorId)) {
            context.Result = new ObjectResult(new {
                error = "This can't be changed while signed in as someone else.",
                code = ErrorCode
            }) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
