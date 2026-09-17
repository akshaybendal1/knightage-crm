using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class ProductStageRepository : IProductStageRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public ProductStageRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<ProductStage>> GetActiveAsync()
    {
        using var connection = _context.CreateConnection();
        var stages = await connection.QueryAsync<ProductStage>("usp_ProductStages_GetActive",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return stages.ToList();
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
        return stage;
    }

    public async Task UpdateAsync(ProductStage stage)
    {
        stage.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductStages_Update",
            new { stage.Id, stage.OrganizationId, stage.Name, stage.SortOrder, stage.IsWon, stage.IsLost, stage.IsDefault }, commandType: CommandType.StoredProcedure);
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_ProductStages_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }
}
