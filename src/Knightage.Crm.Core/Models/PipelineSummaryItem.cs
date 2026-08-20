namespace Knightage.Crm.Core.Models;

public class PipelineSummaryItem
{
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int LeadCount { get; set; }
}
