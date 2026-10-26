using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Knightage.Crm.Infrastructure.Repositories;

public class ProductCategoryRepository : IProductCategoryRepository
{
    // See PipelineStageRepository's cache for the full rationale.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;
    private readonly IMemoryCache _cache;

    public ProductCategoryRepository(DapperContext context, TenantContext tenantContext, IMemoryCache cache)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    private string ActiveCacheKey(Guid organizationId) => $"product-categories:active:{organizationId}";

    public async Task<IReadOnlyList<ProductCategory>> GetActiveAsync()
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var cached = await _cache.GetOrCreateAsync(ActiveCacheKey(organizationId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var connection = _context.CreateConnection();
            var categories = await connection.QueryAsync<ProductCategory>("usp_ProductCategories_GetActive",
                new { OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
            return categories.ToList();
        });
        return cached ?? new List<ProductCategory>();
    }

    public async Task<ProductCategory?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ProductCategory>("usp_ProductCategories_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<ProductCategory> CreateAsync(ProductCategory category)
    {
        category.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductCategories_Create", category, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(category.OrganizationId));
        return category;
    }

    public async Task UpdateAsync(ProductCategory category)
    {
        category.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductCategories_Update",
            new { category.Id, category.OrganizationId, category.Name, category.SortOrder }, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(category.OrganizationId));
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductCategories_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(organizationId));
    }
}
