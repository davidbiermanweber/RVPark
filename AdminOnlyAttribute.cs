using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// Restricts a controller/action to admins (AccessLevel == 3), matching the
// convention already used by AccountController's employee-management actions.
// Not authenticated -> redirect to login; authenticated non-admin -> Forbid.
public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new ChallengeResult(); // cookie auth -> /Account/Login
            return;
        }

        if (user.FindFirst("AccessLevel")?.Value != "3")
        {
            context.Result = new ForbidResult();
        }
    }
}
