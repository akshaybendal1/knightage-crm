using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ILeadTaskRepository
{
    Task<IReadOnlyList<LeadTask>> GetByLeadIdAsync(Guid leadId);
    Task<(IReadOnlyList<LeadTask> Items, int TotalCount)> GetByAssigneePagedAsync(string? assignedToUserId, string? status, int page, int pageSize);
    Task<IReadOnlyList<LeadTask>> GetByDateRangeAsync(string? assignedToUserId, DateTime from, DateTime to);
    Task<int> GetDueOrOverdueCountAsync(string assignedToUserId, DateTime today);
    Task<IReadOnlyList<LeadTask>> GetDueTomorrowUnremindedAsync(DateTime tomorrow);
    Task MarkRemindedAsync(IEnumerable<Guid> taskIds, DateTime sentAtUtc);
    Task<LeadTask?> GetByIdAsync(Guid id);
    Task<LeadTask> CreateAsync(LeadTask task);
    Task UpdateStatusAsync(Guid id, string status, DateTime? completedAtUtc, string? updatedByUserId, DateTime updatedAtUtc);
}
