using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public DashboardRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<PipelineSummaryItem>> GetPipelineSummaryAsync()
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<PipelineSummaryItem>("usp_Dashboard_GetPipelineSummary",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<WonLostCounts> GetWonLostCountsAsync(DateTime? sinceUtc, DateTime? untilUtc)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<WonLostCounts>("usp_Dashboard_GetWonLostCounts",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Since = sinceUtc, Until = untilUtc }, commandType: CommandType.StoredProcedure);
    }

    public async Task<int> GetActivityCountSinceAsync(DateTime sinceUtc)
    {
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("usp_Dashboard_GetActivityCountSince",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Since = sinceUtc }, commandType: CommandType.StoredProcedure);
    }

    public async Task<int> GetOverdueOpenTaskCountAsync(DateTime todayUtc)
    {
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("usp_Dashboard_GetOverdueOpenTaskCount",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Today = todayUtc }, commandType: CommandType.StoredProcedure);
    }

    public async Task<int> GetStuckLeadsCountAsync(DateTime cutoffUtc)
    {
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("usp_Dashboard_GetStuckLeadsCount",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Cutoff = cutoffUtc }, commandType: CommandType.StoredProcedure);
    }
}
