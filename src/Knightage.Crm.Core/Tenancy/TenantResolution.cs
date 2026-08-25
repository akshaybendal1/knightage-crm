namespace Knightage.Crm.Core.Tenancy;

/// <summary>
/// What ITenantDatabaseResolver resolves per request: which database to connect to, and
/// whether the tenant's subscription still allows writes (or anything at all). Status values
/// mirror knightage-platform's SubscriptionStatusCalculator: "Active", "ReadOnly", "Disabled".
/// </summary>
public record TenantResolution(string DatabaseName, string SubscriptionStatus);
