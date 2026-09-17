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
public record SubscriptionDetails(string Status, DateTime? StartDate, DateTime? EndDate, string? AccentColor, bool FieldForceTrackingEnabled);

public interface ISubscriptionStatusProvider
{
    Task<string> GetStatusAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default);

    Task<SubscriptionDetails> GetDetailsAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default);
}
