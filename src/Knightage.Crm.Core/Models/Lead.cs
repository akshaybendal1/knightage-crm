namespace Knightage.Crm.Core.Models;

public class Lead
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public Guid PipelineStageId { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "Manual";
    public string? OwnerUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    // Set on insert (= CreatedAtUtc) and whenever PipelineStageId actually changes.
    // Unlike UpdatedAtUtc (clobbered by any field edit), this is stage-specific --
    // powers "stuck" detection (see DashboardRepository.GetStuckLeadsCountAsync).
    public DateTime? PipelineStageChangedAtUtc { get; set; }

    // Sum of Quantity * UnitPrice across this lead's LeadProducts line items --
    // computed by the repository's SQL, never written back to the DB directly.
    public decimal TotalCost { get; set; }

    // Captured automatically from the browser's Geolocation API when this lead was
    // created via a Field Force Tracking sales entry (Source = "FieldSales") --
    // always null for every other creation path (manual, import, bulk seed).
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime? LocationCapturedAtUtc { get; set; }
}
