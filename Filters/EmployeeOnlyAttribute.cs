using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class EmployeeOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Must be logged in.
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new ChallengeResult();
            return;
        }

        // Only staff accounts may access employee pages.
        // Admins also have Role = Employee, so they are allowed.
        if (user.FindFirst("Role")?.Value != "Employee")
        {
            context.Result = new ForbidResult();
        }
    }
}