namespace Knightage.Crm.Core.Tenancy;

/// <summary>
/// Per-request holder for the organization resolved by TenantResolutionMiddleware from the
/// JWT's org_id claim, read by repositories to scope every query. Scoped, not a swappable
/// abstraction -- no interface needed.
/// </summary>
public class TenantContext
{
    public Guid? OrganizationId { get; set; }

    // Null for every tenant on the shared database (the default) -- set by
    // TenantResolutionMiddleware only when Platform reports this org has been moved to
    // another database (possibly one shared with other tenants -- sharding, not necessarily
    // dedicated). DapperContext checks this to decide which database to actually connect to.
    public string? DatabaseName { get; set; }

    // Null means "the default configured server" -- only set when this org's data has moved
    // to a database on a genuinely different SQL Server than the one this app normally
    // connects to.
    public string? ServerAddress { get; set; }

    public Guid RequireOrganizationId() =>
        OrganizationId ?? throw new InvalidOperationException("No organization has been resolved for this request.");
}
