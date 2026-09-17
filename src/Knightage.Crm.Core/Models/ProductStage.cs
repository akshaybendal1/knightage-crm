namespace Knightage.Crm.Core.Models;

public class ProductStage
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }

    // Pre-selected product stage in the Add Sales Entry form's product-line picker --
    // see SalesEntriesController/sales-entry.ts. Falls back to IsWon if no stage has
    // this set.
    public bool IsDefault { get; set; }
}
