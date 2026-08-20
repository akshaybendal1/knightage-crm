using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class PipelineStageRepository : IPipelineStageRepository
{
    private readonly DapperContext _context;

    public PipelineStageRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PipelineStage>> GetActiveAsync()
    {
        const string sql = @"SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost FROM PipelineStages WHERE IsActive = 1 ORDER BY SortOrder";
        using var connection = _context.CreateConnection();
        var stages = await connection.QueryAsync<PipelineStage>(sql);
        return stages.ToList();
    }

    public async Task<PipelineStage?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost FROM PipelineStages WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PipelineStage>(sql, new { Id = id });
    }

    public async Task<PipelineStage> CreateAsync(PipelineStage stage)
    {
        const string sql = @"INSERT INTO PipelineStages (Id, Name, SortOrder, IsActive, IsWon, IsLost)
                              VALUES (@Id, @Name, @SortOrder, @IsActive, @IsWon, @IsLost)";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, stage);
        return stage;
    }

    public async Task UpdateAsync(PipelineStage stage)
    {
        const string sql = @"UPDATE PipelineStages SET Name = @Name, SortOrder = @SortOrder, IsWon = @IsWon, IsLost = @IsLost
                              WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, stage);
    }
}
