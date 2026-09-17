namespace Knightage.Crm.Core.Models;

public class LeadsByProductItem
{
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int LeadCount { get; set; }
    public decimal TotalValue { get; set; }
}
