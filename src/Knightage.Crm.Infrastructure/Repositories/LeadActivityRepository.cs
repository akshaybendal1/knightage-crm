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

    public async Task<(IReadOnlyList<LeadActivity> Items, bool HasMore)> GetByLeadIdAsync(Guid leadId, int page, int pageSize)
    {
        const string sql = @"SELECT Id, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc
                              FROM LeadActivities WHERE LeadId = @LeadId ORDER BY CreatedAtUtc DESC
                              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        using var connection = _context.CreateConnection();
        var activities = (await connection.QueryAsync<LeadActivity>(sql, new
        {
            LeadId = leadId,
            Skip = (page - 1) * pageSize,
            Take = pageSize + 1
        })).ToList();

        var hasMore = activities.Count > pageSize;
        if (hasMore)
        {
            activities.RemoveAt(activities.Count - 1);
        }

        return (activities, hasMore);
    }

    public async Task<LeadActivity> CreateAsync(LeadActivity activity)
    {
        const string sql = @"INSERT INTO LeadActivities (Id, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc)
                              VALUES (@Id, @LeadId, @Type, @Content, @Metadata, @CreatedByUserId, @CreatedAtUtc)";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, activity);
        return activity;
    }

    public async Task CreateManyAsync(IEnumerable<LeadActivity> activities)
    {
        const string sql = @"INSERT INTO LeadActivities (Id, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc)
                              VALUES (@Id, @LeadId, @Type, @Content, @Metadata, @CreatedByUserId, @CreatedAtUtc)";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, activities);
    }
}
