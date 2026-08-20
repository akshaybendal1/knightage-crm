namespace Knightage.Crm.Core.Models;

public class LeadActivity
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string Type { get; set; } = "Note";
    public string Content { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
