using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Knightage.Crm.Infrastructure.Repositories;

public class MessageTemplateRepository : IMessageTemplateRepository
{
    // See PipelineStageRepository's cache for the full rationale.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;
    private readonly IMemoryCache _cache;

    public MessageTemplateRepository(DapperContext context, TenantContext tenantContext, IMemoryCache cache)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    private string AllCacheKey(Guid organizationId) => $"message-templates:all:{organizationId}";

    public async Task<IReadOnlyList<MessageTemplate>> GetAllAsync()
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var cached = await _cache.GetOrCreateAsync(AllCacheKey(organizationId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var connection = _context.CreateConnection();
            var templates = await connection.QueryAsync<MessageTemplate>("usp_MessageTemplates_GetAll",
                new { OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
            return templates.ToList();
        });
        return cached ?? new List<MessageTemplate>();
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
        _cache.Remove(AllCacheKey(template.OrganizationId));
        return template;
    }

    public async Task UpdateAsync(MessageTemplate template)
    {
        template.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_MessageTemplates_Update",
            new { template.Id, template.OrganizationId, template.Name, template.Body, template.UpdatedByUserId, template.UpdatedAtUtc },
            commandType: CommandType.StoredProcedure);
        _cache.Remove(AllCacheKey(template.OrganizationId));
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_MessageTemplates_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);
        _cache.Remove(AllCacheKey(organizationId));
    }
}
