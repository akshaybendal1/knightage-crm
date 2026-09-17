using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class ReportsRepository : IReportsRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public ReportsRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<LeadTrendPoint>> GetLeadTrendAsync(DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<LeadTrendPoint>("usp_Reports_GetLeadTrend",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Since = sinceUtc, Until = untilUtc, BucketByMonth = bucketByMonth },
            commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<IReadOnlyList<LeadSourceBreakdownItem>> GetLeadSourceBreakdownAsync(DateTime? sinceUtc, DateTime? untilUtc)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<LeadSourceBreakdownItem>("usp_Reports_GetLeadSourceBreakdown",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Since = sinceUtc, Until = untilUtc }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<TeamRawCounts> GetTeamRawCountsAsync(DateTime? sinceUtc, DateTime? untilUtc, DateTime todayUtc)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();

        var leadsCreated = await connection.QueryAsync<UserCount>("usp_Reports_GetLeadsCreatedByUser",
            new { OrganizationId = organizationId, Since = sinceUtc, Until = untilUtc }, commandType: CommandType.StoredProcedure);
        var tasksCompleted = await connection.QueryAsync<UserCount>("usp_Reports_GetTasksCompletedByUser",
            new { OrganizationId = organizationId, Since = sinceUtc, Until = untilUtc }, commandType: CommandType.StoredProcedure);
        var tasksOverdue = await connection.QueryAsync<UserCount>("usp_Reports_GetTasksOverdueByUser",
            new { OrganizationId = organizationId, Today = todayUtc }, commandType: CommandType.StoredProcedure);

        return new TeamRawCounts
        {
            LeadsCreated = leadsCreated.ToList(),
            TasksCompleted = tasksCompleted.ToList(),
            TasksOverdue = tasksOverdue.ToList(),
        };
    }

    public async Task<IReadOnlyList<TaskTrendPoint>> GetTaskCompletionTrendAsync(DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<TaskTrendPoint>("usp_Reports_GetTaskCompletionTrend",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Since = sinceUtc, Until = untilUtc, BucketByMonth = bucketByMonth },
            commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<IReadOnlyList<LeadsByProductItem>> GetLeadsByProductAsync(DateTime? sinceUtc, DateTime? untilUtc)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<LeadsByProductItem>("usp_Reports_GetLeadsByProduct",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Since = sinceUtc, Until = untilUtc }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<IReadOnlyList<LeadsByCategoryItem>> GetLeadsByCategoryAsync(DateTime? sinceUtc, DateTime? untilUtc)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<LeadsByCategoryItem>("usp_Reports_GetLeadsByCategory",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Since = sinceUtc, Until = untilUtc }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<IReadOnlyList<UserCount>> GetLeadsByOwnerRawAsync(DateTime? sinceUtc, DateTime? untilUtc)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<UserCount>("usp_Reports_GetLeadsByOwnerRaw",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Since = sinceUtc, Until = untilUtc }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<IReadOnlyList<LeadSourceDetailItem>> GetLeadDetailsBySourceAsync(string source, DateTime? sinceUtc, DateTime? untilUtc)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<LeadSourceDetailItem>("usp_Reports_GetLeadDetailsBySource",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Source = source, Since = sinceUtc, Until = untilUtc },
            commandType: CommandType.StoredProcedure);
        return items.ToList();
    }
}
