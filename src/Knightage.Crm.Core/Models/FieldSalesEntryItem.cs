namespace Knightage.Crm.Core.Models;

// One row of the Sales Dashboard list -- a Lead with Source = "FieldSales", plus the
// bits the dashboard needs that Lead itself doesn't carry (product names). OwnerDisplayName
// is populated by the controller from IIdentityUserDirectory, same as LeadsController.Export
// already does for the regular Leads export -- CRM's own database has no user names, only ids.
public class FieldSalesEntryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public string? OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime? LocationCapturedAtUtc { get; set; }
    public decimal TotalCost { get; set; }
    public string? Products { get; set; }
}
