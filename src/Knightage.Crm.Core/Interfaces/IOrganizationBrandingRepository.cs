using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IOrganizationBrandingRepository
{
    Task<OrganizationBranding?> GetAsync();
    Task UpsertAsync(string logoContentType, byte[] logoData, string? updatedByUserId);
    Task DeleteAsync();
}
