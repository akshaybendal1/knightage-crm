using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.ScreenAccess;
using Knightage.Crm.Core.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.Services;

/// <summary>
/// Resolves "may this role open this screen?" for the caller's organization: the built-in defaults in
/// <see cref="ScreenCatalog"/> with that organization's stored overrides on top. The overrides are
/// cached briefly per organization (every guarded request asks) and dropped as soon as they change.
/// </summary>
public class ScreenAccessService : IScreenAccessService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly IRoleScreenAccessRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<ScreenAccessService> _logger;

    public ScreenAccessService(IRoleScreenAccessRepository repository, IMemoryCache cache, TenantContext tenantContext, ILogger<ScreenAccessService> logger)
    {
        _repository = repository;
        _cache = cache;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private string CacheKey() => $"screen-access:{_tenantContext.RequireOrganizationId()}";

    private async Task<IReadOnlyDictionary<(string Role, string Screen), bool>> GetOverridesAsync()
    {
        var key = CacheKey();
        if (_cache.TryGetValue(key, out IReadOnlyDictionary<(string Role, string Screen), bool>? cached) && cached is not null)
        {
            return cached;
        }

        IReadOnlyList<ScreenAccessOverride> rows;
        try
        {
            rows = await _repository.GetOverridesAsync();
        }
        catch (SqlException ex) when (ex.Number is 208 or 2812)
        {
            // 208 = invalid object name, 2812 = stored procedure not found: the database has not had
            // sql/051 applied yet. Behave as if nothing was customised (the original role rules) instead
            // of failing every request and locking everyone out; saving still fails until it is applied.
            _logger.LogWarning(ex, "RoleScreenAccess is missing from the database (sql/051 not applied?); using the default screen access.");
            rows = Array.Empty<ScreenAccessOverride>();
        }
        var map = rows.ToDictionary(r => (r.Role, r.ScreenKey), r => r.IsAllowed);
        _cache.Set(key, map, CacheTtl);
        return map;
    }

    public async Task<IReadOnlyList<string>> GetAllowedScreensAsync(string? role)
    {
        if (string.IsNullOrEmpty(role))
        {
            return Array.Empty<string>();
        }
        var overrides = await GetOverridesAsync();
        return ScreenCatalog.All.Where(s => ScreenCatalog.IsAllowed(role, s, overrides)).Select(s => s.Key).ToList();
    }

    public async Task<bool> CanAccessAsync(string? role, string screenKey)
    {
        var screen = ScreenCatalog.Find(screenKey);
        if (screen is null || string.IsNullOrEmpty(role))
        {
            return false;
        }
        return ScreenCatalog.IsAllowed(role, screen, await GetOverridesAsync());
    }

    public async Task<ScreenAccessMatrix> GetMatrixAsync()
    {
        var overrides = await GetOverridesAsync();
        var matrix = new Dictionary<string, IReadOnlyDictionary<string, bool>>();
        foreach (var role in ScreenCatalog.ConfigurableRoles)
        {
            matrix[role] = ScreenCatalog.All.ToDictionary(s => s.Key, s => ScreenCatalog.IsAllowed(role, s, overrides));
        }
        return new ScreenAccessMatrix(matrix);
    }

    public async Task SaveMatrixAsync(IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> allowedByRole, string? updatedByUserId)
    {
        var overrides = new List<ScreenAccessOverride>();
        foreach (var (role, screens) in allowedByRole)
        {
            if (role == ScreenCatalog.Owner)
            {
                throw new ArgumentException("The Owner always has access to every screen, so it can't be changed.");
            }
            if (!ScreenCatalog.IsConfigurableRole(role))
            {
                throw new ArgumentException($"'{role}' is not a role whose access can be changed.");
            }

            foreach (var (screenKey, allowed) in screens)
            {
                var screen = ScreenCatalog.Find(screenKey)
                    ?? throw new ArgumentException($"'{screenKey}' is not a screen.");
                var defaultAllowed = ScreenCatalog.DefaultFor(role, screen);

                if (screen.Kind != ScreenKind.Configurable)
                {
                    if (allowed != defaultAllowed)
                    {
                        throw new ArgumentException($"{screen.Label} can't be changed: it is always {(defaultAllowed ? "available to this role" : "limited to the Owner and Admins")}.");
                    }
                    continue;
                }

                // Only a cell that differs from the default is worth storing.
                if (allowed != defaultAllowed)
                {
                    overrides.Add(new ScreenAccessOverride(role, screenKey, allowed));
                }
            }
        }

        await _repository.ReplaceOverridesAsync(overrides, updatedByUserId);
        _cache.Remove(CacheKey());
    }
}
