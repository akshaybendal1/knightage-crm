namespace Knightage.Crm.Core.Models;

public class MessageTemplate
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
