using System.Data;
using Dapper;
using Knightage.Crm.Core.Customers;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public LeadRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<(IReadOnlyList<Lead> Items, int TotalCount)> GetPagedAsync(Guid? pipelineStageId, Guid? productId, string? ownerUserId, string? search, int page, int pageSize)
    {
        var parameters = new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            PipelineStageId = pipelineStageId,
            ProductId = productId,
            OwnerUserId = ownerUserId,
            // Name/Email/Phone/Company are the fields a user would actually recognize a lead by --
            // Notes/Source are free text, not identifying, so left out of the search surface.
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]")}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        using var connection = _context.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("usp_Leads_GetPaged", parameters, commandType: CommandType.StoredProcedure);
        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<Lead>()).ToList();
        return (items, totalCount);
    }

    public async Task<Lead?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Lead>("usp_Leads_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<Lead> CreateAsync(Lead lead)
    {
        lead.OrganizationId = _tenantContext.RequireOrganizationId();
        lead.IsActive = true;
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Leads_Create", new
        {
            lead.Id, lead.OrganizationId, lead.Name, lead.Email, lead.Phone, lead.Company, lead.PipelineStageId,
            lead.Notes, lead.Source, lead.OwnerUserId, lead.IsActive, lead.CreatedByUserId, lead.CreatedAtUtc,
            lead.PipelineStageChangedAtUtc, lead.Latitude, lead.Longitude, lead.LocationCapturedAtUtc
        }, commandType: CommandType.StoredProcedure);
        return lead;
    }

    public async Task UpdateAsync(Lead lead)
    {
        lead.OrganizationId = _tenantContext.RequireOrganizationId();
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Leads_Update", new
        {
            lead.Id, lead.OrganizationId, lead.Name, lead.Email, lead.Phone, lead.Company, lead.PipelineStageId,
            lead.Notes, lead.OwnerUserId, lead.UpdatedByUserId, lead.UpdatedAtUtc, lead.PipelineStageChangedAtUtc
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateStageAsync(Guid id, Guid pipelineStageId, DateTime stageChangedAtUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Leads_UpdateStage", new
        {
            Id = id,
            PipelineStageId = pipelineStageId,
            StageChangedAtUtc = stageChangedAtUtc,
            OrganizationId = _tenantContext.RequireOrganizationId()
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<Lead>> CreateManyAsync(IEnumerable<Lead> leads)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var list = leads.ToList();

        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("Phone", typeof(string));
        table.Columns.Add("Company", typeof(string));
        table.Columns.Add("PipelineStageId", typeof(Guid));
        table.Columns.Add("Notes", typeof(string));
        table.Columns.Add("Source", typeof(string));
        table.Columns.Add("OwnerUserId", typeof(string));
        table.Columns.Add("CreatedByUserId", typeof(string));
        table.Columns.Add("CreatedAtUtc", typeof(DateTime));
        table.Columns.Add("PipelineStageChangedAtUtc", typeof(DateTime));

        foreach (var lead in list)
        {
            lead.OrganizationId = organizationId;
            lead.IsActive = true;
            table.Rows.Add(
                lead.Id, lead.Name, (object?)lead.Email ?? DBNull.Value, (object?)lead.Phone ?? DBNull.Value,
                (object?)lead.Company ?? DBNull.Value, lead.PipelineStageId, (object?)lead.Notes ?? DBNull.Value, lead.Source,
                (object?)lead.OwnerUserId ?? DBNull.Value, (object?)lead.CreatedByUserId ?? DBNull.Value, lead.CreatedAtUtc,
                (object?)lead.PipelineStageChangedAtUtc ?? DBNull.Value);
        }

        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", organizationId);
        parameters.Add("Leads", table.AsTableValuedParameter("LeadTvp"));

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Leads_CreateMany", parameters, commandType: CommandType.StoredProcedure);
        return list;
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Leads_SetActive",
            new { Id = id, IsActive = isActive, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    // Targeted name lookup for a known set of lead ids -- used by Custom Reports' "Lead"
    // dimension label resolution, which previously pulled up to 100,000 full lead rows
    // (every column, plus a LeadProducts cost join) just to read Id+Name off each one.
    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        using var connection = _context.CreateConnection();
        var rows = await connection.QueryAsync("usp_Leads_GetNamesByIds", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            Ids = string.Join(",", idList)
        }, commandType: CommandType.StoredProcedure);
        return rows.ToDictionary(r => (Guid)r.Id, r => (string)r.Name);
    }

    public async Task<(IReadOnlyList<FieldSalesEntryItem> Items, int TotalCount)> GetFieldSalesAsync(string? ownerUserId, int page, int pageSize)
    {
        var parameters = new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            OwnerUserId = ownerUserId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        using var connection = _context.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("usp_Leads_GetFieldSales", parameters, commandType: CommandType.StoredProcedure);
        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<FieldSalesEntryItem>()).ToList();
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<FieldSalesEntryItem>> GetFieldSalesLocationsAsync(string ownerUserId, DateTime fromUtc, DateTime toUtc)
    {
        var parameters = new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            OwnerUserId = ownerUserId,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };

        using var connection = _context.CreateConnection();
        var rows = await connection.QueryAsync<FieldSalesEntryItem>("usp_Leads_GetFieldSalesLocations", parameters, commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<FieldSalesByRepRawRow>> GetFieldSalesByRepRawAsync(DateTime? sinceUtc, DateTime? untilUtc)
    {
        var parameters = new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            Since = sinceUtc,
            Until = untilUtc
        };

        using var connection = _context.CreateConnection();
        var rows = await connection.QueryAsync<FieldSalesByRepRawRow>("usp_Leads_GetFieldSalesByRep", parameters, commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<LeadMatch>> FindDuplicatesAsync(DuplicateQuery query, Guid? excludeLeadId, int max)
    {
        using var connection = _context.CreateConnection();
        var rows = await connection.QueryAsync<LeadMatch>("usp_Leads_FindDuplicates", new
        {
            OrganizationId = _tenantContext.RequireOrganizationId(),
            query.Email,
            query.PhoneKey,
            query.Name,
            query.Company,
            ExcludeLeadId = excludeLeadId,
            Max = max,
        }, commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<int> CountActiveAsync()
    {
        var parameters = new { OrganizationId = _tenantContext.RequireOrganizationId() };
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("usp_Leads_CountActive", parameters, commandType: CommandType.StoredProcedure);
    }
}
