using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly DapperContext _context;

    public DashboardRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PipelineSummaryItem>> GetPipelineSummaryAsync()
    {
        const string sql = @"SELECT ps.Id AS StageId, ps.Name AS StageName, ps.SortOrder,
                                     (SELECT COUNT(*) FROM Leads l WHERE l.PipelineStageId = ps.Id) AS LeadCount
                              FROM PipelineStages ps
                              WHERE ps.IsActive = 1
                              ORDER BY ps.SortOrder";
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<PipelineSummaryItem>(sql);
        return items.ToList();
    }

    public async Task<WonLostCounts> GetWonLostCountsAsync(DateTime? sinceUtc)
    {
        const string sql = @"SELECT
                                (SELECT COUNT(*) FROM Leads l JOIN PipelineStages ps ON ps.Id = l.PipelineStageId
                                 WHERE ps.IsWon = 1 AND (@Since IS NULL OR l.CreatedAtUtc >= @Since)) AS Won,
                                (SELECT COUNT(*) FROM Leads l JOIN PipelineStages ps ON ps.Id = l.PipelineStageId
                                 WHERE ps.IsLost = 1 AND (@Since IS NULL OR l.CreatedAtUtc >= @Since)) AS Lost";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<WonLostCounts>(sql, new { Since = sinceUtc });
    }

    public async Task<int> GetActivityCountSinceAsync(DateTime sinceUtc)
    {
        const string sql = "SELECT COUNT(*) FROM LeadActivities WHERE CreatedAtUtc >= @Since";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { Since = sinceUtc });
    }

    public async Task<int> GetOverdueOpenTaskCountAsync(DateTime todayUtc)
    {
        const string sql = "SELECT COUNT(*) FROM Tasks WHERE Status = 'Open' AND DueDate < @Today";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { Today = todayUtc });
    }
}
