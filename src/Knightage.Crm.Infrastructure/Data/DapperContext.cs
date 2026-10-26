using System.Data;
using Knightage.Crm.Core.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Knightage.Crm.Infrastructure.Data;

public class DapperContext
{
    private readonly string _connectionString;
    private readonly TenantContext _tenantContext;

    public DapperContext(IConfiguration configuration, TenantContext tenantContext)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
        _tenantContext = tenantContext;
    }

    // Every tenant on the default shared database (unchanged behavior) just gets the
    // configured connection string as-is. A tenant moved elsewhere (TenantContext.DatabaseName
    // set by TenantResolutionMiddleware) gets the same credentials with the catalog swapped,
    // and the server too if it moved to a different one entirely -- same mechanism
    // knightage-inventory-sales' own DapperContext already uses for its (mandatory, not
    // optional) per-tenant databases.
    public IDbConnection CreateConnection()
    {
        if (_tenantContext.DatabaseName is string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = databaseName };
            if (_tenantContext.ServerAddress is string serverAddress)
            {
                builder.DataSource = serverAddress;
            }
            return new SqlConnection(builder.ConnectionString);
        }

        return new SqlConnection(_connectionString);
    }
}
