using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IMeetingRepository
{
    Task<IReadOnlyList<Meeting>> GetByLeadIdAsync(Guid leadId);
    Task<Meeting?> GetByIdAsync(Guid id);
    Task<Meeting> CreateAsync(Meeting meeting);
    Task UpdateAsync(Meeting meeting);
    Task UpdateStatusAsync(Guid id, string status, DateTime? cancelledAtUtc, string? updatedByUserId, DateTime updatedAtUtc);

    // Separate from UpdateAsync/UpdateStatusAsync: written from the best-effort sync
    // branch that runs after the primary write already committed, independent of
    // whatever else that request was doing.
    Task UpdateGoogleSyncAsync(Guid id, string? googleEventId, string googleSyncStatus, DateTime? errorAtUtc);

    // Reminder digest support (ReminderDigestService), mirroring
    // ILeadTaskRepository.GetDueTomorrowUnremindedAsync/MarkRemindedAsync.
    Task<IReadOnlyList<Meeting>> GetStartingSoonUnremindedAsync(DateTime nowUtc, DateTime untilUtc);
    Task MarkRemindedAsync(IEnumerable<Guid> meetingIds, DateTime sentAtUtc);

    // For the Dashboard "my upcoming reminders" view -- a plain read, never touches
    // ReminderSentAtUtc (that's digest-consumption only, see above).
    Task<IReadOnlyList<Meeting>> GetUpcomingForOwnerAsync(string ownerUserId, DateTime nowUtc, DateTime untilUtc);
}
