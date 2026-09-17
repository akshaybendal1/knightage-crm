namespace Knightage.Crm.Core.Tenancy;

/// <summary>
/// Per-request holder for the organization resolved by TenantResolutionMiddleware from the
/// JWT's org_id claim, read by repositories to scope every query. Scoped, not a swappable
/// abstraction -- no interface needed.
/// </summary>
public class TenantContext
{
    public Guid? OrganizationId { get; set; }

    public Guid RequireOrganizationId() =>
        OrganizationId ?? throw new InvalidOperationException("No organization has been resolved for this request.");
}
