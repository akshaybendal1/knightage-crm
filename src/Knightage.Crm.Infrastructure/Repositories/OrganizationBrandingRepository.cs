using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class OrganizationBrandingRepository : IOrganizationBrandingRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public OrganizationBrandingRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<OrganizationBranding?> GetAsync()
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<OrganizationBranding>("usp_OrganizationBranding_Get",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task UpsertAsync(string logoContentType, byte[] logoData, string? updatedByUserId)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_OrganizationBranding_Upsert", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            LogoContentType = logoContentType,
            LogoData = logoData,
            UpdatedByUserId = updatedByUserId,
            UpdatedAtUtc = DateTime.UtcNow
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync()
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_OrganizationBranding_Delete",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }
}
