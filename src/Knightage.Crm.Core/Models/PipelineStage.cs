namespace Knightage.Crm.Core.Models;

public class PipelineStage
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }

    // The stage a Field Force Tracking sales entry lands new leads in -- see
    // SalesEntriesController. Falls back to IsWon if no stage has this set, so
    // existing orgs that configured IsWon before this flag existed keep working.
    public bool IsDefault { get; set; }
}
