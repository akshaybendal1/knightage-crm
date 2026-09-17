using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class SavedCustomReportRepository : ISavedCustomReportRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public SavedCustomReportRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<SavedCustomReport>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        var reports = await connection.QueryAsync<SavedCustomReport>("usp_SavedCustomReports_GetAll",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return reports.ToList();
    }

    public async Task<SavedCustomReport?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SavedCustomReport>("usp_SavedCustomReports_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<SavedCustomReport> CreateAsync(SavedCustomReport report)
    {
        report.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_SavedCustomReports_Create",
            new { report.Id, report.OrganizationId, report.Name, report.DefinitionJson, report.IsActive, report.CreatedByUserId, report.CreatedAtUtc },
            commandType: CommandType.StoredProcedure);
        return report;
    }

    public async Task UpdateAsync(SavedCustomReport report)
    {
        report.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_SavedCustomReports_Update",
            new { report.Id, report.OrganizationId, report.Name, report.DefinitionJson, report.UpdatedByUserId, report.UpdatedAtUtc },
            commandType: CommandType.StoredProcedure);
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_SavedCustomReports_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }
}
