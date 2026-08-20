using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ILeadActivityRepository
{
    Task<(IReadOnlyList<LeadActivity> Items, bool HasMore)> GetByLeadIdAsync(Guid leadId, int page, int pageSize);
    Task<LeadActivity> CreateAsync(LeadActivity activity);
    Task CreateManyAsync(IEnumerable<LeadActivity> activities);
}
