namespace Knightage.Crm.Core.Models;

public class LeadSourceDetailItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public Guid PipelineStageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
