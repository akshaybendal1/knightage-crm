using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Knightage.Crm.Api.Authorization;

/// <summary>
/// Marks an endpoint that a signed-in user may call even when they've been given no access to CRM: what
/// the app needs to say so (screen-access/me), signing a phone in and out of notifications, and calls the
/// other Knightage apps make on a user's behalf.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AllowWithoutAppAccessAttribute : Attribute
{
}

/// <summary>
/// Global: refuses every CRM API call (403, appAccessDenied) from a user whose administrator has turned CRM
/// off for them on Platform's Users screen. The Owner and a PlatformAdmin always pass, as do anonymous
/// endpoints and those marked <see cref="AllowWithoutAppAccessAttribute"/>.
/// </summary>
public class AppAccessFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || metadata.OfType<AllowWithoutAppAccessAttribute>().Any())
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true || user.IsInRole("PlatformAdmin")
            || context.HttpContext.RequestServices.GetRequiredService<TenantContext>().OrganizationId is null)
        {
            return;
        }

        var permissions = await context.HttpContext.RequestServices.GetRequiredService<IPermissionService>().GetCurrentAsync();
        if (!permissions.AppAccess)
        {
            context.Result = new ObjectResult(new
            {
                message = "You don't have access to CRM. Ask your administrator to give you access on Platform → Users.",
                appAccessDenied = true,
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
