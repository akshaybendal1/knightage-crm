namespace Knightage.Crm.Core.Models;

public class LeadExportRow
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
}
