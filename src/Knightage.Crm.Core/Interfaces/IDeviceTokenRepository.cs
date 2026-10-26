namespace Knightage.Crm.Core.Interfaces;

public record DeviceTokenForUser(string UserId, string Token);

public interface IDeviceTokenRepository
{
    Task UpsertAsync(string userId, string token, string platform, DateTime nowUtc);
    Task<IReadOnlyList<DeviceTokenForUser>> GetTokensForUsersAsync(IEnumerable<string> userIds);
    Task RemoveTokenAsync(string token);
}
