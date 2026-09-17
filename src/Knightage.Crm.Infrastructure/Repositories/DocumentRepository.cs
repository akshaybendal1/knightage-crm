using System.Data;
using Dapper;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Knightage.Crm.Infrastructure.Data;

namespace Knightage.Crm.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DapperContext _context;
    private readonly TenantContext _tenantContext;

    public DocumentRepository(DapperContext context, TenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<Document>> GetByLeadIdAsync(Guid leadId)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<Document>("usp_Documents_GetByLeadId",
            new { LeadId = leadId, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Document>("usp_Documents_GetById",
            new { Id = id, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyList<DocumentLineItem>> GetLineItemsAsync(Guid documentId)
    {
        using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<DocumentLineItem>("usp_Documents_GetLineItems",
            new { DocumentId = documentId, OrganizationId = _tenantContext.RequireOrganizationId() }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<Document> CreateAsync(Document document, IReadOnlyList<DocumentLineItem> lineItems)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        document.OrganizationId = organizationId;

        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("ProductName", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("UnitPrice", typeof(decimal));
        table.Columns.Add("LineTotal", typeof(decimal));
        foreach (var item in lineItems)
        {
            item.OrganizationId = organizationId;
            item.DocumentId = document.Id;
            table.Rows.Add(item.Id, item.ProductName, item.Quantity, item.UnitPrice, item.LineTotal);
        }

        var parameters = new DynamicParameters();
        parameters.Add("Id", document.Id);
        parameters.Add("OrganizationId", organizationId);
        parameters.Add("LeadId", document.LeadId);
        parameters.Add("Type", document.Type);
        parameters.Add("Status", document.Status);
        parameters.Add("DiscountPercent", document.DiscountPercent);
        parameters.Add("DiscountAmount", document.DiscountAmount);
        parameters.Add("TaxRatePercent", document.TaxRatePercent);
        parameters.Add("Subtotal", document.Subtotal);
        parameters.Add("TaxAmount", document.TaxAmount);
        parameters.Add("Total", document.Total);
        parameters.Add("CreatedByUserId", document.CreatedByUserId);
        parameters.Add("CreatedAtUtc", document.CreatedAtUtc);
        parameters.Add("SentAtUtc", document.SentAtUtc);
        parameters.Add("PaidAtUtc", document.PaidAtUtc);
        parameters.Add("LineItems", table.AsTableValuedParameter("DocumentLineItemTvp"));

        using var connection = _context.CreateConnection();
        document.Number = await connection.QuerySingleAsync<string>("usp_Documents_Create", parameters, commandType: CommandType.StoredProcedure);
        return document;
    }

    public async Task UpdateStatusAsync(Guid id, string status, DateTime? sentAtUtc, DateTime? paidAtUtc)
    {
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync("usp_Documents_UpdateStatus",
            new { Id = id, Status = status, SentAtUtc = sentAtUtc, PaidAtUtc = paidAtUtc, OrganizationId = _tenantContext.RequireOrganizationId() },
            commandType: CommandType.StoredProcedure);
    }
}
