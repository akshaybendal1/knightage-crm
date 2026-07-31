using System.Net;
using System.Net.Http.Json;
using Knightage.Crm.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Knightage.Crm.Infrastructure.ExternalServices;

/// <summary>
/// Resolves this service's per-tenant database via knightage-platform's tenant directory
/// (GET /api/tenants/{organizationId}), forwarding the caller's own bearer token. Successful
/// resolutions are cached briefly to avoid a platform round-trip on every request; failures are
/// never cached so a just-registered org or a transient platform outage self-heals on the very
/// next request instead of being stuck for the cache TTL.
/// </summary>
public class PlatformTenantDatabaseResolver : ITenantDatabaseResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly string _serviceName;

    public PlatformTenantDatabaseResolver(IHttpClientFactory httpClientFactory, IMemoryCache cache, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _serviceName = configuration["Tenancy:ServiceName"]
            ?? throw new InvalidOperationException("Tenancy:ServiceName is not configured.");
    }

    public async Task<string?> ResolveAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant-db:{_serviceName}:{organizationId}";
        if (_cache.TryGetValue<string>(cacheKey, out var cached))
        {
            return cached;
        }

        var client = _httpClientFactory.CreateClient("Platform");
        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        var response = await client.GetAsync($"api/tenants/{organizationId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        var detail = await response.Content.ReadFromJsonAsync<TenantDetailResponse>(cancellationToken: cancellationToken);
        var databaseName = detail?.Databases
            .FirstOrDefault(d => string.Equals(d.ServiceName, _serviceName, StringComparison.OrdinalIgnoreCase))
            ?.DatabaseName;

        if (databaseName is not null)
        {
            _cache.Set(cacheKey, databaseName, CacheDuration);
        }

        return databaseName;
    }

    private record TenantDetailResponse(TenantResponse Tenant, List<ServiceDatabaseResponse> Databases);

    private record TenantResponse(Guid Id, Guid OrganizationId, string Name, string Slug, string Status);

    private record ServiceDatabaseResponse(Guid Id, Guid TenantId, string ServiceName, string DatabaseName, string Status);
}
