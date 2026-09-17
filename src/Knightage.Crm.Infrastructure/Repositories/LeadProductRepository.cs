using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class LeadProductRepository : ILeadProductRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public LeadProductRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<LeadProduct>> GetByLeadIdAsync(Guid leadId)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<LeadProduct>("usp_LeadProducts_GetByLeadId",
            new { LeadId = leadId, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<LeadProduct?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<LeadProduct>("usp_LeadProducts_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<LeadProduct> AddAsync(LeadProduct leadProduct)
    {
        leadProduct.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_LeadProducts_Add", new
        {
            leadProduct.Id, leadProduct.OrganizationId, leadProduct.LeadId, leadProduct.ProductId,
            leadProduct.Quantity, leadProduct.UnitPrice, leadProduct.CreatedAtUtc,
            leadProduct.ProductStageId, leadProduct.StatusChangedAtUtc
        }, commandType: CommandType.StoredProcedure);
        return leadProduct;
    }

    public async Task UpdateQuantityAsync(Guid id, int quantity)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_LeadProducts_UpdateQuantity",
            new { Id = id, Quantity = quantity, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateProductStageAsync(Guid id, Guid productStageId, DateTime statusChangedAtUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_LeadProducts_UpdateProductStage",
            new { Id = id, ProductStageId = productStageId, StatusChangedAtUtc = statusChangedAtUtc, OrganizationId = _tenantContext.RequireOrganizationId() },
            commandType: CommandType.StoredProcedure);
    }

    public async Task RemoveAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_LeadProducts_Remove",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }
}
