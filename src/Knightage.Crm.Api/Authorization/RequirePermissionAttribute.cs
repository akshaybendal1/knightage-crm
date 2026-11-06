using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Knightage.Crm.Api.Authorization;

/// <summary>
/// The caller must be allowed at least one of the given "area:action" pairs (e.g. "leads:add") -- their
/// role's default with their own exceptions from Platform's Users screen on top (see PermissionService).
/// Several pairs are for endpoints two screens share: Add Sales Entry creates customers too, so
/// POST /api/customers takes "customers:add" or "sales-entry:add".
///
/// An MVC filter, like RequireScreen, because it needs the tenant that TenantResolutionMiddleware resolves
/// after UseAuthorization. Combine with [Authorize].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly (string Feature, string Action)[] _anyOf;

    public RequirePermissionAttribute(params string[] anyOf)
    {
        _anyOf = anyOf.Select(p =>
        {
            var parts = p.Split(':');
            return parts.Length == 2 ? (parts[0], parts[1]) : throw new ArgumentException($"'{p}' is not 'area:action'.");
        }).ToArray();
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissions = await context.HttpContext.RequestServices.GetRequiredService<IPermissionService>().GetCurrentAsync();
        if (!_anyOf.Any(p => permissions.Can(p.Feature, p.Action)))
        {
            context.Result = new ObjectResult(new
            {
                message = "You don't have permission to do this. Ask your administrator to change your access on Platform → Users.",
                permissionDenied = true,
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
