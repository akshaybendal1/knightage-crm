using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Knightage.Crm.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    // See PipelineStageRepository's cache for the full rationale.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;
    private readonly IMemoryCache _cache;

    public ProductRepository(DapperContext context, TenantContext tenantContext, IMemoryCache cache)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    private string AllCacheKey(Guid organizationId) => $"products:all:{organizationId}";

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var cached = await _cache.GetOrCreateAsync(AllCacheKey(organizationId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var connection = _context.CreateConnection();
            var products = await connection.QueryAsync<Product>("usp_Products_GetAll",
                new { OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
            return products.ToList();
        });
        return cached ?? new List<Product>();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>("usp_Products_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Products_Create", new
        {
            product.Id, product.OrganizationId, product.Name, product.Description, product.Category,
            product.Price, product.IsActive, product.CreatedByUserId, product.CreatedAtUtc
        }, commandType: CommandType.StoredProcedure);
        _cache.Remove(AllCacheKey(product.OrganizationId));
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        product.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Products_Update", new
        {
            product.Id, product.OrganizationId, product.Name, product.Description, product.Category,
            product.Price, product.UpdatedByUserId, product.UpdatedAtUtc
        }, commandType: CommandType.StoredProcedure);
        _cache.Remove(AllCacheKey(product.OrganizationId));
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Products_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
        _cache.Remove(AllCacheKey(organizationId));
    }
}
