using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class TimelineRepository : ITimelineRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public TimelineRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<(IReadOnlyList<LeadTimelineItem> Items, int TotalCount)> GetLeadsTimelineAsync(Guid pipelineStageId, Guid? productId, string? ownerUserId, string? search, int page, int pageSize)
    {
        var parameters = new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            StageId = pipelineStageId,
            ProductId = productId,
            OwnerUserId = ownerUserId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]")}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        using var connection = _context.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("usp_Timeline_GetLeadsTimeline", parameters, commandType: CommandType.StoredProcedure);
        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<LeadTimelineItem>()).ToList();
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<LeadProductTimelineItem> Items, int TotalCount)> GetLeadProductsTimelineAsync(Guid productStageId, Guid? productId, string? ownerUserId, string? search, int page, int pageSize)
    {
        var parameters = new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            StageId = productStageId,
            ProductId = productId,
            OwnerUserId = ownerUserId,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]")}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        using var connection = _context.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("usp_Timeline_GetLeadProductsTimeline", parameters, commandType: CommandType.StoredProcedure);
        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<LeadProductTimelineItem>()).ToList();
        return (items, totalCount);
    }
}
