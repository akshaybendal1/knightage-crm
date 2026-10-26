using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Knightage.Crm.Infrastructure.Repositories;

public class PipelineStageRepository : IPipelineStageRepository
{
    // Read on essentially every Leads/Dashboard/Lead-Detail request but written to only
    // when an admin edits stages -- an ideal cache candidate. TTL is a safety net only;
    // every write below removes this org's entry immediately, so staleness never exceeds
    // one request in flight (see the CRM subscription-cache incidents this cache pattern
    // is deliberately copied from -- TTL alone caused stale-data bugs twice already).
    // In-process IMemoryCache is safe here only because this app runs as a single IIS
    // instance, not load-balanced across multiple servers -- revisit if that changes.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;
    private readonly IMemoryCache _cache;

    public PipelineStageRepository(DapperContext context, TenantContext tenantContext, IMemoryCache cache)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    private string ActiveCacheKey(Guid organizationId) => $"pipeline-stages:active:{organizationId}";

    public async Task<IReadOnlyList<PipelineStage>> GetActiveAsync()
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var cached = await _cache.GetOrCreateAsync(ActiveCacheKey(organizationId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var connection = _context.CreateConnection();
            var stages = await connection.QueryAsync<PipelineStage>("usp_PipelineStages_GetActive",
                new { OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
            return stages.ToList();
        });
        return cached ?? new List<PipelineStage>();
    }

    public async Task<PipelineStage?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PipelineStage>("usp_PipelineStages_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<PipelineStage> CreateAsync(PipelineStage stage)
    {
        stage.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_PipelineStages_Create", stage, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(stage.OrganizationId));
        return stage;
    }

    public async Task UpdateAsync(PipelineStage stage)
    {
        stage.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_PipelineStages_Update",
            new { stage.Id, stage.OrganizationId, stage.Name, stage.SortOrder, stage.IsWon, stage.IsLost, stage.IsDefault }, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(stage.OrganizationId));
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_PipelineStages_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
        _cache.Remove(ActiveCacheKey(organizationId));
    }
}
