namespace Knightage.Crm.Core.Models;

public class OrganizationBranding
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string LogoContentType { get; set; } = string.Empty;
    public byte[] LogoData { get; set; } = Array.Empty<byte>();
    public string? UpdatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
