namespace Knightage.Crm.Core.Models;

public class LeadProduct
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid LeadId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;

    // Snapshot of Product.Price at the moment this line item was added -- deliberately
    // does not track the catalog price afterward, so a lead's recorded cost never
    // silently drifts when someone edits a product's price later.
    public decimal UnitPrice { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    // A customizable per-org stage, mirroring Lead.PipelineStageId exactly
    // (see ProductStage) -- was a fixed Active/Won/Lost string before.
    public Guid ProductStageId { get; set; }

    // Set on insert (= CreatedAtUtc) and whenever ProductStageId actually changes.
    // Powers "stuck" detection (see DashboardRepository.GetStuckLeadsCountAsync) --
    // never touched by an unrelated Quantity edit.
    public DateTime? StatusChangedAtUtc { get; set; }

    // Only populated by the joined list query, matching Meeting.LeadName/LeadTask.LeadName.
    public string? ProductName { get; set; }

    // Only populated by the joined list query, matching ProductName above.
    public string? ProductStageName { get; set; }
}
