using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class RoleScreenAccessRepository : IRoleScreenAccessRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public RoleScreenAccessRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<ScreenAccessOverride>> GetOverridesAsync()
    {
        using var connection = _context.CreateConnection();
        var rows = await connection.QueryAsync<ScreenAccessOverride>("usp_RoleScreenAccess_GetByOrganization", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId()
        }, commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task ReplaceOverridesAsync(IReadOnlyCollection<ScreenAccessOverride> overrides, string? updatedByUserId)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_RoleScreenAccess_Replace", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            // "Role:ScreenKey:1,..." -- roles and screen keys never contain ':' or ','.
            Entries = string.Join(",", overrides.Select(o => $"{o.Role}:{o.ScreenKey}:{(o.IsAllowed ? 1 : 0)}")),
            UpdatedByUserId = updatedByUserId,
            NowUtc = DateTime.UtcNow
        }, commandType: CommandType.StoredProcedure);
    }
}
