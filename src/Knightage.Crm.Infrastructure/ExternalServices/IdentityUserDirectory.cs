using System.Net.Http.Json;
using Knightage.Crm.Core.Interfaces;

namespace Knightage.Crm.Infrastructure.ExternalServices;

/// <summary>
/// Resolves the calling org's users (for reminder-email addressing) via knightage-identity's
/// GET /api/users, forwarding the caller's own bearer token -- the same pattern
/// PlatformTenantDatabaseResolver uses to call knightage-platform. No caching: call frequency
/// is already throttled to roughly once per organization per day by ReminderDigestGate.
/// </summary>
public class IdentityUserDirectory : IIdentityUserDirectory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IdentityUserDirectory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<OrgUserEmail>> GetOrganizationUsersAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("Identity");
        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        var response = await client.GetAsync("api/users", cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<OrgUserEmail>>(cancellationToken: cancellationToken);
        return users ?? [];
    }
}
