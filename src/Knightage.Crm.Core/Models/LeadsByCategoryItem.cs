namespace Knightage.Crm.Core.Models;

public class LeadsByCategoryItem
{
    public string Category { get; set; } = string.Empty;
    public int LeadCount { get; set; }
    public decimal TotalValue { get; set; }
}
