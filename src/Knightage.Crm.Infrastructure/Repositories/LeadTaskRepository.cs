using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class LeadTaskRepository : ILeadTaskRepository
{
    private readonly DapperContext _context;

    public LeadTaskRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LeadTask>> GetByLeadIdAsync(Guid leadId)
    {
        const string sql = @"SELECT Id, LeadId, Title, Description, DueDate, Status, AssignedToUserId,
                                     CreatedByUserId, CreatedAtUtc, CompletedAtUtc
                              FROM Tasks WHERE LeadId = @LeadId ORDER BY DueDate ASC";
        using var connection = _context.CreateConnection();
        var tasks = await connection.QueryAsync<LeadTask>(sql, new { LeadId = leadId });
        return tasks.ToList();
    }

    public async Task<IReadOnlyList<LeadTask>> GetByAssigneeAsync(string? assignedToUserId, string? status)
    {
        var sql = @"SELECT t.Id, t.LeadId, t.Title, t.Description, t.DueDate, t.Status, t.AssignedToUserId,
                            t.CreatedByUserId, t.CreatedAtUtc, t.CompletedAtUtc, l.Name AS LeadName
                     FROM Tasks t
                     JOIN Leads l ON l.Id = t.LeadId
                     WHERE (@AssignedToUserId IS NULL OR t.AssignedToUserId = @AssignedToUserId)";
        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND t.Status = @Status";
        }
        sql += " ORDER BY t.DueDate ASC";

        using var connection = _context.CreateConnection();
        var tasks = await connection.QueryAsync<LeadTask>(sql, new { AssignedToUserId = assignedToUserId, Status = status });
        return tasks.ToList();
    }

    public async Task<LeadTask?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, LeadId, Title, Description, DueDate, Status, AssignedToUserId,
                                     CreatedByUserId, CreatedAtUtc, CompletedAtUtc
                              FROM Tasks WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<LeadTask>(sql, new { Id = id });
    }

    public async Task<LeadTask> CreateAsync(LeadTask task)
    {
        const string sql = @"INSERT INTO Tasks (Id, LeadId, Title, Description, DueDate, Status, AssignedToUserId,
                                                  CreatedByUserId, CreatedAtUtc, CompletedAtUtc)
                              VALUES (@Id, @LeadId, @Title, @Description, @DueDate, @Status, @AssignedToUserId,
                                      @CreatedByUserId, @CreatedAtUtc, @CompletedAtUtc)";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, task);
        return task;
    }

    public async Task UpdateStatusAsync(Guid id, string status, DateTime? completedAtUtc)
    {
        const string sql = "UPDATE Tasks SET Status = @Status, CompletedAtUtc = @CompletedAtUtc WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, Status = status, CompletedAtUtc = completedAtUtc });
    }
}
