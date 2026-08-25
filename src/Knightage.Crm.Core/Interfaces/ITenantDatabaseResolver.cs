using Knightage.Crm.Core.Tenancy;

namespace Knightage.Crm.Core.Interfaces;

public interface ITenantDatabaseResolver
{
    /// <summary>
    /// Resolves this service's database name (and current subscription status) for the given
    /// organization by asking knightage-platform. Returns null if the organization isn't found
    /// or this service hasn't been provisioned for it yet -- never throws for those expected
    /// cases.
    /// </summary>
    Task<TenantResolution?> ResolveAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default);
}
