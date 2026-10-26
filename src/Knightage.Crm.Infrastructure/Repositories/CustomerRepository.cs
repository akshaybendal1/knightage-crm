using System.Data;
using Dapper;
using Knightage.Crm.Core.Customers;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public CustomerRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    // The upsert proc returns the customer plus a WasCreated flag in one row.
    private sealed class UpsertRow : Customer
    {
        public bool WasCreated { get; set; }
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        var customers = await connection.QueryAsync<Customer>("usp_Customers_GetAll",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return customers.ToList();
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Customer>("usp_Customers_GetById",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Id = id }, commandType: CommandType.StoredProcedure);
    }

    public async Task<(Customer Customer, bool WasCreated)> UpsertAsync(CustomerDraft draft, Guid? leadId, string source, string? createdByUserId)
    {
        using var connection = _context.CreateConnection();
        var row = await connection.QuerySingleAsync<UpsertRow>("usp_Customers_Upsert", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            draft.Name,
            draft.Company,
            draft.Email,
            draft.Phone,
            draft.PhoneKey,
            Source = source,
            LeadId = leadId,
            CreatedByUserId = createdByUserId,
            NowUtc = DateTime.UtcNow,
        }, commandType: CommandType.StoredProcedure);
        return (row, row.WasCreated);
    }

    public async Task LinkLeadAsync(Guid leadId, Guid customerId)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_CustomerLeads_Link",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), LeadId = leadId, CustomerId = customerId, NowUtc = DateTime.UtcNow },
            commandType: CommandType.StoredProcedure);
    }

    public async Task SetInventoryLinkAsync(Guid id, Guid inventoryCustomerId)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Customers_SetInventoryLink",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Id = id, InventoryCustomerId = inventoryCustomerId, NowUtc = DateTime.UtcNow },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<Customer>> GetPendingInventoryAsync(int max)
    {
        using var connection = _context.CreateConnection();
        var customers = await connection.QueryAsync<Customer>("usp_Customers_GetPendingInventory",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Max = max }, commandType: CommandType.StoredProcedure);
        return customers.ToList();
    }

    public async Task<int> CountPendingInventoryAsync()
    {
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("usp_Customers_CountPendingInventory",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<WonLeadSummary>> GetWonLeadsWithoutCustomerAsync(int max)
    {
        using var connection = _context.CreateConnection();
        var leads = await connection.QueryAsync<WonLeadSummary>("usp_Leads_GetWonWithoutCustomer",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Max = max }, commandType: CommandType.StoredProcedure);
        return leads.ToList();
    }

    public async Task<Customer?> GetForLeadAsync(Guid leadId)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Customer>("usp_Customers_GetForLead",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), LeadId = leadId }, commandType: CommandType.StoredProcedure);
    }

    public async Task UnlinkLeadAsync(Guid leadId)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_CustomerLeads_Unlink",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), LeadId = leadId }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string? term, int max)
    {
        // \ is the LIKE escape character in usp_Customers_Search, so a typed % _ [ or \ is matched literally.
        var escaped = (term ?? string.Empty).Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
        using var connection = _context.CreateConnection();
        var rows = await connection.QueryAsync<Customer>("usp_Customers_Search", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            Pattern = "%" + escaped + "%",
            Prefix = escaped + "%",
            Max = Math.Clamp(max, 1, 25),
        }, commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CustomerMatch>> FindDuplicatesAsync(DuplicateQuery query, int max)
    {
        using var connection = _context.CreateConnection();
        var rows = await connection.QueryAsync<CustomerMatch>("usp_Customers_FindDuplicates", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            query.Email,
            query.PhoneKey,
            query.Name,
            query.Company,
            Max = max,
        }, commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<int> CountWonLeadsWithoutCustomerAsync()
    {
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("usp_Leads_CountWonWithoutCustomer",
            new { OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<(IReadOnlyList<WonLeadListItem> Items, int TotalCount)> GetWonLeadsWithoutCustomerPageAsync(string? search, int page, int pageSize)
    {
        using var connection = _context.CreateConnection();
        var rows = (await connection.QueryAsync<WonLeadPageRow>("usp_Leads_GetWonWithoutCustomerPaged",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Search = search, Offset = (page - 1) * pageSize, PageSize = pageSize },
            commandType: CommandType.StoredProcedure)).ToList();
        var items = rows.Select(r => new WonLeadListItem(r.Id, r.Name, r.Company, r.Email, r.Phone, r.StageName, r.WonAtUtc)).ToList();
        return (items, rows.Count > 0 ? rows[0].TotalCount : 0);
    }

    public async Task<IReadOnlyList<WonLeadSummary>> GetWonLeadsWithoutCustomerByIdsAsync(IReadOnlyCollection<Guid> leadIds)
    {
        if (leadIds.Count == 0)
        {
            return Array.Empty<WonLeadSummary>();
        }

        using var connection = _context.CreateConnection();
        var leads = await connection.QueryAsync<WonLeadSummary>("usp_Leads_GetWonWithoutCustomerByIds",
            new { OrganizationId = _tenantContext.RequireOrganizationId(), Ids = string.Join(',', leadIds) },
            commandType: CommandType.StoredProcedure);
        return leads.ToList();
    }

    private sealed class WonLeadPageRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? StageName { get; set; }
        public DateTime WonAtUtc { get; set; }
        public int TotalCount { get; set; }
    }
}
