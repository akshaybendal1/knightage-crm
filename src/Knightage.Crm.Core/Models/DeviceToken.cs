namespace Knightage.Crm.Core.Models;

public class DeviceToken
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}
