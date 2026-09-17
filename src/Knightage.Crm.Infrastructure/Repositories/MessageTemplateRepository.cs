using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class MessageTemplateRepository : IMessageTemplateRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public MessageTemplateRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<MessageTemplate>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        var templates = await connection.QueryAsync<MessageTemplate>("usp_MessageTemplates_GetAll",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return templates.ToList();
    }

    public async Task<MessageTemplate?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MessageTemplate>("usp_MessageTemplates_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<MessageTemplate> CreateAsync(MessageTemplate template)
    {
        template.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_MessageTemplates_Create",
            new { template.Id, template.OrganizationId, template.Name, template.Body, template.IsActive, template.CreatedByUserId, template.CreatedAtUtc },
            commandType: CommandType.StoredProcedure);
        return template;
    }

    public async Task UpdateAsync(MessageTemplate template)
    {
        template.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_MessageTemplates_Update",
            new { template.Id, template.OrganizationId, template.Name, template.Body, template.UpdatedByUserId, template.UpdatedAtUtc },
            commandType: CommandType.StoredProcedure);
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_MessageTemplates_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }
}
