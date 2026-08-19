using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ILeadActivityRepository
{
    Task<IReadOnlyList<LeadActivity>> GetByLeadIdAsync(Guid leadId);
    Task<LeadActivity> CreateAsync(LeadActivity activity);
}
