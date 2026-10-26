using Knightage.Crm.Core.Customers;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ILeadRepository
{
    Task<(IReadOnlyList<Lead> Items, int TotalCount)> GetPagedAsync(Guid? pipelineStageId, Guid? productId, string? ownerUserId, string? search, int page, int pageSize);
    Task<Lead?> GetByIdAsync(Guid id);
    Task<Lead> CreateAsync(Lead lead);
    Task UpdateAsync(Lead lead);
    Task UpdateStageAsync(Guid id, Guid pipelineStageId, DateTime stageChangedAtUtc);
    Task<IReadOnlyList<Lead>> CreateManyAsync(IEnumerable<Lead> leads);
    Task SetActiveAsync(Guid id, bool isActive);
    Task<IReadOnlyDictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids);

    // ownerUserId: NULL for the org-wide Owner/Admin Sales Dashboard view, set to the
    // caller's own id for a Sales user's own view -- see usp_Leads_GetFieldSales.
    Task<(IReadOnlyList<FieldSalesEntryItem> Items, int TotalCount)> GetFieldSalesAsync(string? ownerUserId, int page, int pageSize);

    // All of one rep's sales-entry locations in a date range, unpaged -- backs the Sales
    // Dashboard's admin-only coverage map/area view. See usp_Leads_GetFieldSalesLocations.
    Task<IReadOnlyList<FieldSalesEntryItem>> GetFieldSalesLocationsAsync(string ownerUserId, DateTime fromUtc, DateTime toUtc);

    // Org-wide per-rep sales totals in a date range -- backs the Sales Dashboard's
    // "Sales by rep" chart. See usp_Leads_GetFieldSalesByRep.
    Task<IReadOnlyList<FieldSalesByRepRawRow>> GetFieldSalesByRepRawAsync(DateTime? sinceUtc, DateTime? untilUtc);

    // Count of active (non-soft-deleted) Leads for the current org -- backs the plan's
    // record-limit check and usage banner. A lightweight dedicated count, not GetPagedAsync's
    // TotalCount (that would mean fetching a page of rows just to read a count).
    Task<int> CountActiveAsync();

    // Active leads that look like the one being typed (same email, phone, or name + company), strongest match first.
    // excludeLeadId leaves out the lead being edited. See usp_Leads_FindDuplicates.
    Task<IReadOnlyList<LeadMatch>> FindDuplicatesAsync(DuplicateQuery query, Guid? excludeLeadId, int max);
}
