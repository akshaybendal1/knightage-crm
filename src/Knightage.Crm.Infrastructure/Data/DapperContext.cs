using System.Data;
using Knightage.Crm.Core.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Knightage.Crm.Infrastructure.Data;

public class DapperContext
{
    private readonly string _serverConnectionString;
    private readonly CurrentTenantContext _tenantContext;

    public DapperContext(IConfiguration configuration, CurrentTenantContext tenantContext)
    {
        _serverConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
        _tenantContext = tenantContext;
    }

    public IDbConnection CreateConnection()
    {
        // TenantResolutionMiddleware guarantees this is set (or the request was already
        // short-circuited with 401/503) before any repository runs -- this is an assertion,
        // not an expected path.
        var databaseName = _tenantContext.DatabaseName
            ?? throw new InvalidOperationException("No tenant database has been resolved for this request.");

        var builder = new SqlConnectionStringBuilder(_serverConnectionString) { InitialCatalog = databaseName };
        return new SqlConnection(builder.ConnectionString);
    }
}
