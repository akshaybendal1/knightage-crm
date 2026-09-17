using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IDocumentRepository
{
    Task<IReadOnlyList<Document>> GetByLeadIdAsync(Guid leadId);
    Task<Document?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<DocumentLineItem>> GetLineItemsAsync(Guid documentId);
    Task<Document> CreateAsync(Document document, IReadOnlyList<DocumentLineItem> lineItems);
    Task UpdateStatusAsync(Guid id, string status, DateTime? sentAtUtc, DateTime? paidAtUtc);
}
