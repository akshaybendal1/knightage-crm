namespace Knightage.Crm.Core.Models;

public class Meeting
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid LeadId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string OwnerUserId { get; set; } = string.Empty;
    public string? GoogleEventId { get; set; }
    public string GoogleSyncStatus { get; set; } = "NotSynced";
    public DateTime? GoogleSyncErrorAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    // Only populated by a future cross-lead query (a join), matching LeadTask.LeadName.
    public string? LeadName { get; set; }

    // Set once a reminder digest email covering this meeting has actually been sent --
    // see ReminderDigestService. Null means "not yet reminded."
    public DateTime? ReminderSentAtUtc { get; set; }
}
