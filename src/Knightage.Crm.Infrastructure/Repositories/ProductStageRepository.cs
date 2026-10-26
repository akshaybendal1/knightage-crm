using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Knightage.Crm.Infrastructure.Repositories;

public class ProductStageRepository : IProductStageRepository
{
    // See PipelineStageRepository's cache for the full rationale -- same pattern, same
    // read/write frequency shape, same single-instance-deployment caveat.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;
    private readonly IMemoryCache _cache;

    public ProductStageRepository(DapperContext context, TenantContext tenantContext, IMemoryCache cache)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    private string ActiveCacheKey(Guid organizationId) => $"product-stages:active:{organizationId}";

    public async Task<IReadOnlyList<ProductStage>> GetActiveAsync()
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var cached = await _cache.GetOrCreateAsync(ActiveCacheKey(organizationId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var connection = _context.CreateConnection();
            var stages = await connection.QueryAsync<ProductStage>("usp_ProductStages_GetActive",
                new { OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
            return stages.ToList();
        });
        return cached ?? new List<ProductStage>();
    }

    public async Task<ProductStage?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ProductStage>("usp_ProductStages_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<ProductStage> CreateAsync(ProductStage stage)
    {
        stage.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductStages_Create", stage, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(stage.OrganizationId));
        return stage;
    }

    public async Task UpdateAsync(ProductStage stage)
    {
        stage.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductStages_Update",
            new { stage.Id, stage.OrganizationId, stage.Name, stage.SortOrder, stage.IsWon, stage.IsLost, stage.IsDefault }, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(stage.OrganizationId));
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductStages_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(organizationId));
    }
}
