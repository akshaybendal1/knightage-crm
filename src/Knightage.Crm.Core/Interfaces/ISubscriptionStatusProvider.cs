namespace Knightage.Crm.Core.Interfaces;

/// <summary>
/// Status values mirror knightage-platform's SubscriptionStatusCalculator exactly --
/// keep the two in sync if either changes.
/// </summary>
public static class SubscriptionStatus
{
    public const string Active = "Active";
    public const string ReadOnly = "ReadOnly";
    public const string Disabled = "Disabled";
    public const string NotSubscribed = "NotSubscribed";
}

// AccentColor and FieldForceTrackingEnabled ride along on this record even though
// neither is subscription data -- both come from the exact same cached Platform
// tenant lookup (GET api/tenants/{id}), and adding a second interface/call just to
// fetch one more field from the same response would be pure overhead.
//
// Unlike Status (which fails OPEN to Active so a Platform outage never locks an org
// out of its own data), FieldForceTrackingEnabled fails CLOSED to false on a lookup
// failure -- an unreachable Platform must never silently unlock a paid add-on.
//
// PlanMaxUsers/PlanMaxRecords fail OPEN to null (= unlimited), same reasoning as
// Status -- a Platform outage must never block a paying customer from creating
// records or managing their team just because the limit lookup itself failed.
//
// DedicatedDatabaseName is null for every tenant on the shared database (the vast
// majority) -- only set once a tenant has been moved to another database via
// knightage-platform's tenant migration feature (which, despite the name here, isn't
// necessarily single-tenant-dedicated -- it can be a shard shared with other tenants).
// DedicatedServerAddress is null unless that move was also to a different SQL Server than
// this app normally connects to. IsMigrating fails OPEN to false on a Platform lookup
// failure, same as Status -- but note this is different from "safe": a genuine mid-copy
// outage failing open here would let requests through against a database that's about to be
// repointed. Acceptable because IsMigrating is only ever true for the few minutes an
// admin-triggered migration is actually running, not a steady-state condition like the
// other fields.
//
// ServiceEnabled reflects a PlatformAdmin explicitly turning CRM off for this tenant
// (TenantServiceDatabases.Status = "Disabled", set from platform's Tenants admin page) --
// fails OPEN to true, same reasoning as Status: a Platform outage must never lock a tenant
// out of a service they already have just because the enablement check itself failed.
public record SubscriptionDetails(string Status, DateTime? StartDate, DateTime? EndDate, string? AccentColor, bool FieldForceTrackingEnabled, int? PlanMaxUsers, int? PlanMaxRecords, string? DedicatedDatabaseName, string? DedicatedServerAddress, bool IsMigrating, bool ServiceEnabled);

public interface ISubscriptionStatusProvider
{
    Task<string> GetStatusAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default);

    Task<SubscriptionDetails> GetDetailsAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops this organization's cached details immediately instead of waiting out the
    /// provider's own TTL. Called by knightage-platform (via InternalController) right when
    /// a tenant migration starts and again when it finishes, so the maintenance-window
    /// block and the repointed database take effect on this CRM process's very next request
    /// for that tenant -- confirmed by a real incident where a write shortly after a
    /// migration silently landed in the shared database (about to be purged) because this
    /// process's cache hadn't yet learned the tenant had moved.
    /// </summary>
    void InvalidateCache(Guid organizationId);
}
