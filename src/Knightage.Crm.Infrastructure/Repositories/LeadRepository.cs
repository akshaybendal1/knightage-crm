using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly DapperContext _context;

    public LeadRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Lead>> GetAllAsync(Guid? pipelineStageId = null)
    {
        var sql = @"SELECT Id, Name, Email, Phone, Company, PipelineStageId, Notes, Source, CreatedAtUtc
                     FROM Leads";
        if (pipelineStageId.HasValue)
        {
            sql += " WHERE PipelineStageId = @PipelineStageId";
        }
        sql += " ORDER BY CreatedAtUtc DESC";

        using var connection = _context.CreateConnection();
        var leads = await connection.QueryAsync<Lead>(sql, new { PipelineStageId = pipelineStageId });
        return leads.ToList();
    }

    public async Task<Lead?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, Name, Email, Phone, Company, PipelineStageId, Notes, Source, CreatedAtUtc
                              FROM Leads WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Lead>(sql, new { Id = id });
    }

    public async Task<Lead> CreateAsync(Lead lead)
    {
        const string sql = @"INSERT INTO Leads (Id, Name, Email, Phone, Company, PipelineStageId, Notes, Source, CreatedAtUtc)
                              VALUES (@Id, @Name, @Email, @Phone, @Company, @PipelineStageId, @Notes, @Source, @CreatedAtUtc)";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, lead);
        return lead;
    }

    public async Task UpdateAsync(Lead lead)
    {
        const string sql = @"UPDATE Leads SET Name = @Name, Email = @Email, Phone = @Phone, Company = @Company,
                              PipelineStageId = @PipelineStageId, Notes = @Notes WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, lead);
    }

    public async Task<IReadOnlyList<Lead>> CreateManyAsync(IEnumerable<Lead> leads)
    {
        const string sql = @"INSERT INTO Leads (Id, Name, Email, Phone, Company, PipelineStageId, Notes, Source, CreatedAtUtc)
                              VALUES (@Id, @Name, @Email, @Phone, @Company, @PipelineStageId, @Notes, @Source, @CreatedAtUtc)";
        var list = leads.ToList();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, list);
        return list;
    }
}
