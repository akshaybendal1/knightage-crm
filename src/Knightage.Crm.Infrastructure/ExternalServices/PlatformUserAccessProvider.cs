using System.Net.Http.Json;
using System.Text.Json;
using Knightage.Crm.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.ExternalServices;

/// <summary>
/// Reads the signed-in user's exceptions from knightage-platform's GET /api/access/me (forwarding their
/// token, like PlatformSubscriptionStatusProvider). Asked on every request that checks a permission, so it
/// is cached per user for a minute -- a change on the Users screen takes effect within that.
///
/// Fails soft: if Platform can't answer, the last answer seen for this user (kept for a day) is used, so a
/// short Platform outage doesn't hand back what an admin took away; with nothing seen yet, the role defaults
/// apply (exactly the behaviour before per-user access) rather than locking everyone out.
/// </summary>
public class PlatformUserAccessProvider : IUserAccessProvider
{
    private static readonly TimeSpan SuccessTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FailureTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LastKnownTtl = TimeSpan.FromDays(1);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlatformUserAccessProvider> _logger;

    public PlatformUserAccessProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<PlatformUserAccessProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UserAccessSnapshot> GetAsync(Guid organizationId, Guid userId, string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var key = $"user-access:{organizationId}:{userId}";
        if (_cache.TryGetValue(key, out UserAccessSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        var lastKnownKey = $"user-access-last:{organizationId}:{userId}";
        try
        {
            var client = _httpClientFactory.CreateClient("Platform");
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/access/me");
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<AccessResponse>(JsonOptions, cancellationToken);
            var snapshot = new UserAccessSnapshot(
                body?.Apps ?? new Dictionary<string, bool>(),
                body?.Overrides ?? new Dictionary<string, bool>());
            _cache.Set(key, snapshot, SuccessTtl);
            _cache.Set(lastKnownKey, snapshot, LastKnownTtl);
            return snapshot;
        }
        catch (Exception ex)
        {
            var fallback = _cache.TryGetValue(lastKnownKey, out UserAccessSnapshot? lastKnown) && lastKnown is not null ? lastKnown : UserAccessSnapshot.None;
            _logger.LogWarning(ex, "User access lookup failed for user {UserId} in organization {OrganizationId}; using {Fallback}.",
                userId, organizationId, ReferenceEquals(fallback, UserAccessSnapshot.None) ? "the role defaults" : "the last answer seen");
            _cache.Set(key, fallback, FailureTtl);
            return fallback;
        }
    }

    private record AccessResponse(Dictionary<string, bool>? Apps, Dictionary<string, bool>? Overrides);
}
