namespace Knightage.Crm.Core.Models;

public class PipelineStage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
}
