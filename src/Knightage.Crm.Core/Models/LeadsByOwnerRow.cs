namespace Knightage.Crm.Core.Models;

public class LeadsByOwnerRow
{
    public string? OwnerUserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int LeadCount { get; set; }
}
