using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

// Called by knightage-platform only, right when a tenant migration starts and again when it
// finishes -- see TenantMigrationController's invalidator calls there. PlatformAdmin-gated
// the same way every other cross-tenant administrative action already is; there's no separate
// service-to-service credential here because CRM and Platform already trust the same
// identity-issued JWT (same Issuer/Audience/Key), so forwarding the PlatformAdmin's own token
// from the Platform request that triggered this is sufficient authorization on its own.
[Authorize(Roles = "PlatformAdmin")]
[ApiController]
[Route("api/internal/tenant-cache")]
public class InternalController : ControllerBase
{
    private readonly ISubscriptionStatusProvider _subscriptionStatusProvider;

    public InternalController(ISubscriptionStatusProvider subscriptionStatusProvider)
    {
        _subscriptionStatusProvider = subscriptionStatusProvider;
    }

    [HttpPost("{organizationId:guid}/invalidate")]
    public IActionResult Invalidate(Guid organizationId)
    {
        _subscriptionStatusProvider.InvalidateCache(organizationId);
        return NoContent();
    }
}
