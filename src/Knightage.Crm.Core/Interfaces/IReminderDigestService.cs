namespace Knightage.Crm.Core.Interfaces;

/// <summary>
/// Checks for tasks due tomorrow and meetings starting soon that haven't been reminded
/// yet and, if any are found, emails each assignee/owner one combined digest. Designed
/// to be called from an authenticated request's own pipeline (see
/// TenantResolutionMiddleware) rather than a standalone background worker -- there is no
/// service-to-service auth in this system, so a request's own forwarded bearer token is
/// the only way to resolve tenant users' emails from knightage-identity.
/// </summary>
public interface IReminderDigestService
{
    Task RunIfDueAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default);
}
