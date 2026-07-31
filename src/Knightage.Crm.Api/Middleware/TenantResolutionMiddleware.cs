using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;

namespace Knightage.Crm.Api.Middleware;

/// <summary>
/// Resolves the caller's tenant database once per request (from the JWT's org_id claim) and
/// stores it on the scoped CurrentTenantContext, which DapperContext reads when building its
/// connection. Placed after UseAuthorization() so anonymous endpoints (client-config, health)
/// never trigger a platform lookup, and unauthorized requests never reach here at all.
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
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var orgIdClaim = context.User.FindFirst("org_id")?.Value;
            if (!Guid.TryParse(orgIdClaim, out var organizationId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Token is missing a valid org_id claim." });
                return;
            }

            var authorizationHeader = context.Request.Headers.Authorization.ToString();
            var databaseName = await resolver.ResolveAsync(organizationId, authorizationHeader, context.RequestAborted);
            if (databaseName is null)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { message = "This organization's tenant database is not available yet." });
                return;
            }

            tenantContext.DatabaseName = databaseName;
        }

        await _next(context);
    }
}
