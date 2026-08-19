using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class LeadActivityRepository : ILeadActivityRepository
{
    private readonly DapperContext _context;

    public LeadActivityRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LeadActivity>> GetByLeadIdAsync(Guid leadId)
    {
        const string sql = @"SELECT Id, LeadId, Type, Content, CreatedByUserId, CreatedAtUtc
                              FROM LeadActivities WHERE LeadId = @LeadId ORDER BY CreatedAtUtc DESC";
        using var connection = _context.CreateConnection();
        var activities = await connection.QueryAsync<LeadActivity>(sql, new { LeadId = leadId });
        return activities.ToList();
    }

    public async Task<LeadActivity> CreateAsync(LeadActivity activity)
    {
        const string sql = @"INSERT INTO LeadActivities (Id, LeadId, Type, Content, CreatedByUserId, CreatedAtUtc)
                              VALUES (@Id, @LeadId, @Type, @Content, @CreatedByUserId, @CreatedAtUtc)";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, activity);
        return activity;
    }
}
