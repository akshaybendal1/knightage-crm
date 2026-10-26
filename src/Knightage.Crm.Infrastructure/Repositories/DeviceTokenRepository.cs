using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class DeviceTokenRepository : IDeviceTokenRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public DeviceTokenRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task UpsertAsync(string userId, string token, string platform, DateTime nowUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_DeviceTokens_Upsert", new
        {
            Id = Guid.NewGuid(),
            OrganizationId = _tenantContext.RequireOrganizationId(),
            UserId = userId,
            Token = token,
            Platform = platform,
            NowUtc = nowUtc
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<DeviceTokenForUser>> GetTokensForUsersAsync(IEnumerable<string> userIds)
    {
        var idList = userIds.ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<DeviceTokenForUser>();
        }

        using var connection = _context.CreateConnection();
        var tokens = await connection.QueryAsync<DeviceTokenForUser>("usp_DeviceTokens_GetTokensForUsers", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            UserIds = string.Join(",", idList)
        }, commandType: CommandType.StoredProcedure);
        return tokens.ToList();
    }

    public async Task RemoveTokenAsync(string token)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_DeviceTokens_DeleteByToken", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            Token = token
        }, commandType: CommandType.StoredProcedure);
    }
}
