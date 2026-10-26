using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Knightage.Crm.Infrastructure.Repositories;

public class OrganizationBrandingRepository : IOrganizationBrandingRepository
{
    // Fetched on essentially every page load (logo + accent color) but changed only when
    // an admin re-uploads a logo -- see PipelineStageRepository's cache for the full
    // rationale. The cached value includes the logo's raw bytes, which is deliberate: the
    // whole point is to skip re-reading that BLOB from SQL on every request, not just the
    // small metadata columns.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;
    private readonly IMemoryCache _cache;

    public OrganizationBrandingRepository(DapperContext context, TenantContext tenantContext, IMemoryCache cache)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    private string CacheKey(Guid organizationId) => $"org-branding:{organizationId}";

    public async Task<OrganizationBranding?> GetAsync()
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        // GetOrCreateAsync can't cache a null factory result as "found", so a missing
        // branding row is represented by a sentinel and unwrapped back to null below --
        // still avoids re-querying SQL for orgs that have never uploaded a logo.
        var cached = await _cache.GetOrCreateAsync(CacheKey(organizationId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var connection = _context.CreateConnection();
            var branding = await connection.QuerySingleOrDefaultAsync<OrganizationBranding>("usp_OrganizationBranding_Get",
                new { OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
            return branding ?? OrganizationBranding.NotFound;
        });
        return ReferenceEquals(cached, OrganizationBranding.NotFound) ? null : cached;
    }

    public async Task UpsertAsync(string logoContentType, byte[] logoData, string? updatedByUserId)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_OrganizationBranding_Upsert", new
        {
            OrganizationId = organizationId,
            LogoContentType = logoContentType,
            LogoData = logoData,
            UpdatedByUserId = updatedByUserId,
            UpdatedAtUtc = DateTime.UtcNow
        }, commandType: CommandType.StoredProcedure);
        _cache.Remove(CacheKey(organizationId));
    }

    public async Task DeleteAsync()
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_OrganizationBranding_Delete",
            new { OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
        _cache.Remove(CacheKey(organizationId));
    }
}
