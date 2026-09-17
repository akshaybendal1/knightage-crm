namespace Knightage.Crm.Core.Models;

public class DocumentLineItem
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DocumentId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
