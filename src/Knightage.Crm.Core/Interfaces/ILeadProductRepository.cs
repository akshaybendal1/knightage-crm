using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ILeadProductRepository
{
    Task<IReadOnlyList<LeadProduct>> GetByLeadIdAsync(Guid leadId);
    Task<LeadProduct?> GetByIdAsync(Guid id);
    Task<LeadProduct> AddAsync(LeadProduct leadProduct);
    Task UpdateQuantityAsync(Guid id, int quantity);
    Task UpdateProductStageAsync(Guid id, Guid productStageId, DateTime statusChangedAtUtc);

    // Real hard delete -- the one deliberate exception to this codebase's otherwise
    // universal soft-deactivate convention. A line item has no business identity of
    // its own outside its parent lead (like an invoice line), unlike a Product/Lead/
    // Task/Meeting, which are all soft-deactivated or status-toggled instead.
    Task RemoveAsync(Guid id);
}
