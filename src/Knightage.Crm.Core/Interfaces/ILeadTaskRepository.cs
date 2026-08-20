using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ILeadTaskRepository
{
    Task<IReadOnlyList<LeadTask>> GetByLeadIdAsync(Guid leadId);
    Task<IReadOnlyList<LeadTask>> GetByAssigneeAsync(string? assignedToUserId, string? status);
    Task<LeadTask?> GetByIdAsync(Guid id);
    Task<LeadTask> CreateAsync(LeadTask task);
    Task UpdateStatusAsync(Guid id, string status, DateTime? completedAtUtc);
}
