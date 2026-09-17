using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class PipelineStageRepository : IPipelineStageRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public PipelineStageRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<PipelineStage>> GetActiveAsync()
    {
        using var connection = _context.CreateConnection();
        var stages = await connection.QueryAsync<PipelineStage>("usp_PipelineStages_GetActive",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return stages.ToList();
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
        return stage;
    }

    public async Task UpdateAsync(PipelineStage stage)
    {
        stage.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_PipelineStages_Update",
            new { stage.Id, stage.OrganizationId, stage.Name, stage.SortOrder, stage.IsWon, stage.IsLost, stage.IsDefault }, commandType: CommandType.StoredProcedure);
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_PipelineStages_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }
}
