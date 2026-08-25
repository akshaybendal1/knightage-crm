using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Microsoft.AspNetCore.Authorization;

namespace Knightage.Crm.Api.Middleware;

/// <summary>
/// Resolves the caller's tenant database once per request (from the JWT's org_id claim) and
/// stores it on the scoped CurrentTenantContext, which DapperContext reads when building its
/// connection. Placed after UseAuthorization() so unauthorized requests never reach here at all.
/// Only runs for endpoints that actually require authorization -- the Angular app's HTTP
/// interceptor attaches the bearer token to every request once a user is logged in, including
/// anonymous ones like /api/client-config, so IsAuthenticated alone isn't a safe gate: a real
/// incident showed a broken/unprovisioned tenant taking down the anonymous bootstrap endpoint
/// with it, which should never depend on tenant resolution at all.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentTenantContext tenantContext, ITenantDatabaseResolver resolver)
    {
        var endpoint = context.GetEndpoint();
        var requiresAuthorization = endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null
            && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;

        if (requiresAuthorization && context.User.Identity?.IsAuthenticated == true)
        {
            var orgIdClaim = context.User.FindFirst("org_id")?.Value;
            if (!Guid.TryParse(orgIdClaim, out var organizationId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Token is missing a valid org_id claim." });
                return;
            }

            var authorizationHeader = context.Request.Headers.Authorization.ToString();
            var resolution = await resolver.ResolveAsync(organizationId, authorizationHeader, context.RequestAborted);
            if (resolution is null)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { message = "This organization's tenant database is not available yet." });
                return;
            }

            // "Disabled" blocks everything; "ReadOnly" only blocks writes (every mutating
            // endpoint in this API is GET or POST -- PUT/DELETE aren't used, see
            // TasksController/PipelineStagesController/LeadsController), so a GET still
            // succeeds and existing data stays visible instead of the app going dark outright.
            if (resolution.SubscriptionStatus == "Disabled")
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "This organization's subscription has ended. Contact your account manager to reactivate." });
                return;
            }

            if (resolution.SubscriptionStatus == "ReadOnly" && !HttpMethods.IsGet(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "This organization's subscription has expired. The app is read-only until it's renewed." });
                return;
            }

            tenantContext.DatabaseName = resolution.DatabaseName;
        }

        await _next(context);
    }
}
