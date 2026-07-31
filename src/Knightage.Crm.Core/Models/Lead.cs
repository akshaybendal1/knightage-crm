namespace Knightage.Crm.Core.Models;

public class Lead
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public Guid PipelineStageId { get; set; }
    public string? Notes { get; set; }
    public string Source { get; set; } = "Manual";
    public DateTime CreatedAtUtc { get; set; }
}
