namespace Knightage.Crm.Core.Tenancy;

/// <summary>
/// Per-request holder for the tenant database resolved by TenantResolutionMiddleware, read by
/// DapperContext. Scoped, not a swappable abstraction -- no interface needed.
/// </summary>
public class CurrentTenantContext
{
    public string? DatabaseName { get; set; }
}
