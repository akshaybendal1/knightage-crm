using System.Net.Http.Json;
using System.Text.Json;
using Knightage.Crm.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.ExternalServices;

/// <summary>
/// Checks the calling org's subscription status via knightage-platform's
/// GET /api/tenants/{organizationId}, same forwarded-auth pattern as IdentityUserDirectory.
/// This sits on every authenticated request (not just a once-a-day digest), so unlike that
/// call it's cached and fails open: a slow or unreachable Platform must never turn into every
/// CRM request stalling or 500ing. A short cache also applies to failures, so a real Platform
/// outage doesn't hammer it with a retry on every single request while still recovering
/// quickly once it's back.
/// </summary>
public class PlatformSubscriptionStatusProvider : ISubscriptionStatusProvider
{
    private static readonly TimeSpan SuccessTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan FailureTtl = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlatformSubscriptionStatusProvider> _logger;

    public PlatformSubscriptionStatusProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<PlatformSubscriptionStatusProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetStatusAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var details = await GetDetailsAsync(organizationId, authorizationHeader, cancellationToken);
        return details.Status;
    }

    public async Task<SubscriptionDetails> GetDetailsAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"subscription-status:{organizationId}";
        if (_cache.TryGetValue(cacheKey, out SubscriptionDetails? cached) && cached is not null)
        {
            return cached;
        }

        SubscriptionDetails details;
        TimeSpan ttl;
        try
        {
            var client = _httpClientFactory.CreateClient("Platform");
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            var response = await client.GetAsync($"api/tenants/{organizationId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<TenantResponse>(JsonOptions, cancellationToken);
            details = new SubscriptionDetails(
                body?.Subscription?.Status ?? SubscriptionStatus.Active,
                body?.Subscription?.StartDate,
                body?.Subscription?.EndDate,
                body?.Tenant?.AccentColor,
                body?.Tenant?.FieldForceTrackingEnabled ?? false);
            ttl = SuccessTtl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Subscription status lookup failed for organization {OrganizationId}; treating as Active.", organizationId);
            // FieldForceTrackingEnabled = false here (unlike Status = Active above) --
            // see the fail-closed note on SubscriptionDetails.
            details = new SubscriptionDetails(SubscriptionStatus.Active, null, null, null, false);
            ttl = FailureTtl;
        }

        _cache.Set(cacheKey, details, ttl);
        return details;
    }

    private record TenantResponse(TenantSummary? Tenant, SubscriptionSummary? Subscription);
    private record TenantSummary(string? AccentColor, bool FieldForceTrackingEnabled);
    private record SubscriptionSummary(string Status, DateTime? StartDate, DateTime? EndDate);
}
