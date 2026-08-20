namespace Knightage.Crm.Core.Models;

public class LeadTask
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Open";
    public string AssignedToUserId { get; set; } = string.Empty;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    // Only populated by the cross-lead "my tasks" query (a join), so the UI can show
    // which lead a task belongs to without a second round trip per row.
    public string? LeadName { get; set; }
}
