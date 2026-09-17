namespace Knightage.Crm.Core.Interfaces;

public record OrganizationDetails(Guid Id, string Name, string? ContactEmail, string? ContactPhone, string? Address);

public interface IIdentityOrganizationClient
{
    Task<OrganizationDetails> GetAsync(string? authorizationHeader, CancellationToken cancellationToken = default);

    Task UpdateAsync(string name, string? contactEmail, string? contactPhone, string? address, string? authorizationHeader, CancellationToken cancellationToken = default);
}
