namespace Knightage.Crm.Core.Models;

public class PipelineSummaryItem
{
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int LeadCount { get; set; }

    // Sum of Quantity * UnitPrice across LeadProducts for leads in this stage --
    // same no-status-filter math as Lead.TotalCost, aggregated per stage.
    public decimal LeadValue { get; set; }
}
