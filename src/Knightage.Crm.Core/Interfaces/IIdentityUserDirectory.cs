namespace Knightage.Crm.Core.Interfaces;

public record OrgUserEmail(string Id, string DisplayName, string Email);

public interface IIdentityUserDirectory
{
    Task<IReadOnlyList<OrgUserEmail>> GetOrganizationUsersAsync(string? authorizationHeader, CancellationToken cancellationToken = default);
}
