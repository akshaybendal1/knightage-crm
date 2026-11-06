using System.Security.Claims;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Knightage.Crm.Api.Authorization;

/// <summary>
/// Replaces a hard-coded <c>[Authorize(Roles = "Owner,Admin")]</c> with "the caller's role must have
/// this screen" according to the organization's Access Control settings (defaults first, so an
/// organization that never customised anything behaves exactly as the role check did).
///
/// An MVC filter rather than an authorization policy on purpose: it needs the tenant, and
/// <c>TenantResolutionMiddleware</c> runs after <c>UseAuthorization</c> but before the endpoint, so
/// only code that runs inside the endpoint can look up this organization's settings. Combine it
/// with <c>[Authorize]</c>, which still does the authentication.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireScreenAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _screenKey;

    public RequireScreenAttribute(string screenKey)
    {
        _screenKey = screenKey;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // A screen whose access can also be set per user (Platform's Users screen) goes by the user's own
        // View permission; the locked ones (Team, Organization, Access Control) stay by role.
        bool allowed;
        if (PermissionCatalog.Find(_screenKey) is not null)
        {
            allowed = await context.HttpContext.RequestServices.GetRequiredService<IPermissionService>().CanAsync(_screenKey, PermissionActions.View);
        }
        else
        {
            var service = context.HttpContext.RequestServices.GetRequiredService<IScreenAccessService>();
            allowed = await service.CanAccessAsync(user.FindFirst(ClaimTypes.Role)?.Value, _screenKey);
        }

        if (!allowed)
        {
            context.Result = new ObjectResult(new { message = "You don't have access to this. Ask an administrator to change it in Access Control or on Platform → Users.", permissionDenied = true })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
