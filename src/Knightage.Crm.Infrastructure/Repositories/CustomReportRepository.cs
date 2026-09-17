using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class CustomReportRepository : ICustomReportRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public CustomReportRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<CustomReportRow>> RunAsync(CustomReportQuery query)
    {
        using var connection = _context.CreateConnection();
        var rows = await connection.QueryAsync<CustomReportRow>("usp_CustomReports_Run", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            Dimension1 = query.Dimensions[0],
            Dimension2 = query.Dimensions.Count > 1 ? query.Dimensions[1] : null,
            BucketByMonth = query.BucketByMonth,
            Since = query.SinceUtc,
            Until = query.UntilUtc,
            StageIds = query.StageIds is { Count: > 0 } ? string.Join(",", query.StageIds) : null,
            OwnerUserIds = query.OwnerUserIds is { Count: > 0 } ? string.Join(",", query.OwnerUserIds) : null,
            ProductIds = query.ProductIds is { Count: > 0 } ? string.Join(",", query.ProductIds) : null,
            Sources = query.Sources is { Count: > 0 } ? string.Join(",", query.Sources) : null,
        }, commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }
}
