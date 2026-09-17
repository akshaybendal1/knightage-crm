using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class LeadTaskRepository : ILeadTaskRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public LeadTaskRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<LeadTask>> GetByLeadIdAsync(Guid leadId)
    {
        using var connection = _context.CreateConnection();
        var tasks = await connection.QueryAsync<LeadTask>("usp_Tasks_GetByLeadId",
            new { LeadId = leadId, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return tasks.ToList();
    }

    public async Task<(IReadOnlyList<LeadTask> Items, int TotalCount)> GetByAssigneePagedAsync(string? assignedToUserId, string? status, int page, int pageSize)
    {
        var parameters = new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            AssignedToUserId = assignedToUserId,
            Status = status,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        using var connection = _context.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("usp_Tasks_GetByAssigneePaged", parameters, commandType: CommandType.StoredProcedure);
        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<LeadTask>()).ToList();
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<LeadTask>> GetByDateRangeAsync(string? assignedToUserId, DateTime from, DateTime to)
    {
        using var connection = _context.CreateConnection();
        var tasks = await connection.QueryAsync<LeadTask>("usp_Tasks_GetByDateRange",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), AssignedToUserId = assignedToUserId, From = from, To = to },
            commandType: CommandType.StoredProcedure);
        return tasks.ToList();
    }

    public async Task<int> GetDueOrOverdueCountAsync(string assignedToUserId, DateTime today)
    {
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("usp_Tasks_GetDueOrOverdueCount",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), AssignedToUserId = assignedToUserId, Today = today },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<LeadTask>> GetDueTomorrowUnremindedAsync(DateTime tomorrow)
    {
        using var connection = _context.CreateConnection();
        var tasks = await connection.QueryAsync<LeadTask>("usp_Tasks_GetDueTomorrowUnreminded",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Tomorrow = tomorrow }, commandType: CommandType.StoredProcedure);
        return tasks.ToList();
    }

    public async Task MarkRemindedAsync(IEnumerable<Guid> taskIds, DateTime sentAtUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Tasks_MarkReminded", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            TaskIds = string.Join(",", taskIds),
            SentAtUtc = sentAtUtc
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task<LeadTask?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<LeadTask>("usp_Tasks_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<LeadTask> CreateAsync(LeadTask task)
    {
        task.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Tasks_Create", new
        {
            task.Id, task.OrganizationId, task.LeadId, task.Title, task.Description, task.DueDate,
            task.Status, task.AssignedToUserId, task.CreatedByUserId, task.CreatedAtUtc, task.CompletedAtUtc
        }, commandType: CommandType.StoredProcedure);
        return task;
    }

    public async Task UpdateStatusAsync(Guid id, string status, DateTime? completedAtUtc, string? updatedByUserId, DateTime updatedAtUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Tasks_UpdateStatus", new
        {
            Id = id,
            Status = status,
            CompletedAtUtc = completedAtUtc,
            UpdatedByUserId = updatedByUserId,
            UpdatedAtUtc = updatedAtUtc,
            OrganizationId = _tenantContext.RequireOrganizationId()
        }, commandType: CommandType.StoredProcedure);
    }
}
