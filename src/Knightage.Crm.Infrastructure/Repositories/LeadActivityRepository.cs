using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class LeadActivityRepository : ILeadActivityRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public LeadActivityRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<(IReadOnlyList<LeadActivity> Items, bool HasMore)> GetByLeadIdAsync(Guid leadId, int page, int pageSize)
    {
        using var connection = _context.CreateConnection();
        var activities = (await connection.QueryAsync<LeadActivity>("usp_LeadActivities_GetByLeadId", new
        {
            LeadId = leadId,
            OrganizationId = _tenantContext.RequireOrganizationId(),
            Skip = (page - 1) * pageSize,
            Take = pageSize + 1
        }, commandType: CommandType.StoredProcedure)).ToList();

        var hasMore = activities.Count > pageSize;
        if (hasMore)
        {
            activities.RemoveAt(activities.Count - 1);
        }

        return (activities, hasMore);
    }

    public async Task<LeadActivity> CreateAsync(LeadActivity activity)
    {
        activity.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_LeadActivities_Create", activity, commandType: CommandType.StoredProcedure);
        return activity;
    }

    public async Task CreateManyAsync(IEnumerable<LeadActivity> activities)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var list = activities.ToList();

        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("LeadId", typeof(Guid));
        table.Columns.Add("Type", typeof(string));
        table.Columns.Add("Content", typeof(string));
        table.Columns.Add("Metadata", typeof(string));
        table.Columns.Add("CreatedByUserId", typeof(string));
        table.Columns.Add("CreatedAtUtc", typeof(DateTime));

        foreach (var activity in list)
        {
            activity.OrganizationId = organizationId;
            table.Rows.Add(
                activity.Id, activity.LeadId, activity.Type, activity.Content,
                (object?)activity.Metadata ?? DBNull.Value, (object?)activity.CreatedByUserId ?? DBNull.Value, activity.CreatedAtUtc);
        }

        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", organizationId);
        parameters.Add("Activities", table.AsTableValuedParameter("LeadActivityTvp"));

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_LeadActivities_CreateMany", parameters, commandType: CommandType.StoredProcedure);
    }
}
