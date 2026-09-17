namespace Knightage.Crm.Core.Models;

public class LeadProductTimelineItem
{
    public Guid LeadProductId { get; set; }
    public Guid LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public Guid ProductStageId { get; set; }
    public string ProductStageName { get; set; } = string.Empty;
    public DateTime StatusChangedAtUtc { get; set; }

    // Computed by the controller from StatusChangedAtUtc, not stored -- matches this
    // codebase's elapsed-time convention (see DashboardRepository.GetStuckLeadsCountAsync).
    public int DaysInStatus { get; set; }
}
