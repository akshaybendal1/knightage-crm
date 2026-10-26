using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Api.Middleware;

/// <summary>
/// Resolves the caller's organization once per request (from the JWT's org_id claim) and
/// stores it on the scoped TenantContext, which every repository reads to scope its
/// queries. Most tenants share one CRM database (row-level OrganizationId filtering);
/// a tenant migrated to its own dedicated database (rare -- see knightage-platform's
/// tenant migration feature) gets TenantContext.DatabaseName set too, which DapperContext
/// uses to connect there instead. Placed after UseAuthorization() so unauthorized
/// requests never reach here at all. Only runs for endpoints that actually require
/// authorization -- the Angular app's HTTP interceptor attaches the bearer token to every
/// request once a user is logged in, including anonymous ones like /api/client-config, so
/// IsAuthenticated alone isn't a safe gate.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete
    };

    // A Disabled org must still be able to see WHY (the banner/Organization page's
    // GET) and ask for help (the subscription-request POST) -- otherwise the block
    // hides its own explanation and escape hatch behind itself.
    private static bool IsSubscriptionEscapeHatch(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/organization", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HttpMethods.IsGet(context.Request.Method)
            || path.EndsWith("/subscription-request", StringComparison.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, IReminderDigestService reminderDigestService, ISubscriptionStatusProvider subscriptionStatusProvider)
    {
        var endpoint = context.GetEndpoint();
        var requiresAuthorization = endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null
            && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;

        Guid? resolvedOrganizationId = null;
        string? authorizationHeader = null;

        if (requiresAuthorization && context.User.Identity?.IsAuthenticated == true)
        {
            var orgIdClaim = context.User.FindFirst("org_id")?.Value;
            if (!Guid.TryParse(orgIdClaim, out var organizationId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Token is missing a valid org_id claim." });
                return;
            }

            tenantContext.OrganizationId = organizationId;
            resolvedOrganizationId = organizationId;
            authorizationHeader = context.Request.Headers.Authorization.ToString();

            // GetDetailsAsync, not GetStatusAsync -- same cached Platform call either way
            // (GetStatusAsync just extracts .Status from this), but this also needs
            // DedicatedDatabaseName/IsMigrating below.
            var details = await subscriptionStatusProvider.GetDetailsAsync(organizationId, authorizationHeader, context.RequestAborted);

            // A tenant mid-migration is blocked entirely, reads included -- a read could
            // otherwise observe a half-copied state. This is a short-lived window (an
            // admin-triggered copy, not a steady-state condition), unlike the
            // subscription-status blocks below which can persist indefinitely.
            if (details.IsMigrating)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { message = "Maintenance in progress for this organization. Try again shortly." });
                return;
            }

            tenantContext.DatabaseName = details.DedicatedDatabaseName;
            tenantContext.ServerAddress = details.DedicatedServerAddress;

            // Blocks writes during the post-expiry grace period and everything once that
            // grace period has also lapsed. Reads stay open in both states so a ReadOnly org
            // can still see its own data, and Active is the default whenever the status
            // lookup itself fails (see PlatformSubscriptionStatusProvider) -- an org is never
            // locked out by a Platform hiccup.
            var status = details.Status;
            var blocked = !IsSubscriptionEscapeHatch(context)
                && (status == SubscriptionStatus.Disabled
                    || (status == SubscriptionStatus.ReadOnly && MutatingMethods.Contains(context.Request.Method)));
            if (blocked)
            {
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = status == SubscriptionStatus.Disabled
                        ? "This organization's subscription has expired. Contact your administrator to renew."
                        : "This organization's subscription is in its grace period -- viewing is available, but changes are disabled until it's renewed.",
                    subscriptionStatus = status
                });
                return;
            }

            // A PlatformAdmin explicitly disabling this service for the tenant (via
            // platform's Tenants admin page) is a deliberate access decision, not a billing
            // grace period -- block reads and writes both, same posture as the Disabled
            // subscription case above and the IsMigrating block, not the softer ReadOnly one.
            if (!details.ServiceEnabled && !IsSubscriptionEscapeHatch(context))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "This service has been disabled for your organization. Contact your administrator.",
                    serviceDisabled = true
                });
                return;
            }
        }

        await _next(context);

        if (resolvedOrganizationId is Guid orgId)
        {
            // Runs after the real response is already built, so it can never delay or
            // interfere with it -- and, awaited inline (not a detached Task.Run), it
            // stays within this request's own DI scope, so the scoped DapperContext and
            // the forwarded-auth HttpClient call it makes both just work safely.
            try
            {
                await reminderDigestService.RunIfDueAsync(orgId, authorizationHeader, context.RequestAborted);
            }
            catch (Exception ex)
            {
                // Best-effort, mirrors TeamService's welcome-email try/catch: a reminder
                // failure must never surface as a failure of the actual request -- but it
                // still gets logged so a broken integration isn't invisible.
                _logger.LogError(ex, "Task reminder digest check failed for organization {OrganizationId}.", orgId);
            }
        }
    }
}
