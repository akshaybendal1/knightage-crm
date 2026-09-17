namespace Knightage.Crm.Core.Models;

public class LeadTimelineItem
{
    public Guid LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string Source { get; set; } = "Manual";
    public string? OwnerUserId { get; set; }
    public decimal TotalCost { get; set; }
    public Guid PipelineStageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public DateTime StageChangedAtUtc { get; set; }

    // Computed by the controller from StageChangedAtUtc, not stored -- matches this
    // codebase's elapsed-time convention (see DashboardRepository.GetStuckLeadsCountAsync).
    public int DaysInStage { get; set; }
}
