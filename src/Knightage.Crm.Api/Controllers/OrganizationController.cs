using System.Security.Claims;
using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

// "My organization" -- name/contact details (proxied to knightage-identity) plus
// subscription status, backing both the app-wide "not subscribed" banner (any
// authenticated user) and the Owner/Admin-only Organization Info settings page.
[Authorize]
[ApiController]
[Route("api/organization")]
public class OrganizationController : ControllerBase
{
    private readonly IIdentityOrganizationClient _organizationClient;
    private readonly ISubscriptionStatusProvider _subscriptionStatusProvider;
    private readonly TenantContext _tenantContext;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(
        IIdentityOrganizationClient organizationClient,
        ISubscriptionStatusProvider subscriptionStatusProvider,
        TenantContext tenantContext,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<OrganizationController> logger)
    {
        _organizationClient = organizationClient;
        _subscriptionStatusProvider = subscriptionStatusProvider;
        _tenantContext = tenantContext;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganization(CancellationToken cancellationToken)
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();
        var organizationId = _tenantContext.RequireOrganizationId();

        try
        {
            // Already computed once per request by TenantResolutionMiddleware -- this
            // is a cache hit (see PlatformSubscriptionStatusProvider), not a second
            // Platform round-trip.
            var subscription = await _subscriptionStatusProvider.GetDetailsAsync(organizationId, authorizationHeader, cancellationToken);
            var organization = await _organizationClient.GetAsync(authorizationHeader, cancellationToken);

            return Ok(new
            {
                organization.Name,
                organization.ContactEmail,
                organization.ContactPhone,
                organization.Address,
                subscriptionStatus = subscription.Status,
                subscriptionStartDate = subscription.StartDate,
                subscriptionEndDate = subscription.EndDate,
                accentColor = subscription.AccentColor,
                fieldForceTrackingEnabled = subscription.FieldForceTrackingEnabled,
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load organization details for {OrganizationId}.", organizationId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Could not load organization details right now." });
        }
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPut]
    public async Task<IActionResult> UpdateOrganization(UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();

        try
        {
            await _organizationClient.UpdateAsync(request.Name, request.ContactEmail, request.ContactPhone, request.Address, authorizationHeader, cancellationToken);
            return NoContent();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to update organization details for {OrganizationId}.", _tenantContext.RequireOrganizationId());
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Could not save organization details right now." });
        }
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("subscription-request")]
    public async Task<IActionResult> RequestSubscription(CancellationToken cancellationToken)
    {
        var platformAdminEmail = _configuration["App:PlatformAdminEmail"];
        if (string.IsNullOrWhiteSpace(platformAdminEmail))
        {
            _logger.LogError("App:PlatformAdminEmail is not configured; cannot send subscription request.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Subscription requests aren't available right now. Try again later." });
        }

        var organizationId = _tenantContext.RequireOrganizationId();
        var requesterName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown user";
        var requesterEmail = User.FindFirst("email")?.Value ?? "unknown";

        string organizationName;
        try
        {
            var organization = await _organizationClient.GetAsync(Request.Headers.Authorization.ToString(), cancellationToken);
            organizationName = organization.Name;
        }
        catch (HttpRequestException)
        {
            organizationName = "(name unavailable)";
        }

        var subject = $"Subscription request: {organizationName}";
        var body = $"""
            {requesterName} ({requesterEmail}) requested a subscription for their organization.

            Organization: {organizationName}
            Organization ID: {organizationId}
            Requested: {DateTime.UtcNow:u}

            Set it up from the Tenants page: https://platform.knightagetech.com/tenants
            """;

        try
        {
            await _emailSender.SendAsync(platformAdminEmail, subject, body, cancellationToken);
            return Ok(new { message = "Request sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription request email for organization {OrganizationId}.", organizationId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Couldn't send the request. Try again in a moment." });
        }
    }
}
