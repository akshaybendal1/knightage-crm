using System.Net;
using System.Net.Http.Json;
using Knightage.Crm.Core.Exceptions;
using Knightage.Crm.Core.Interfaces;

namespace Knightage.Crm.Infrastructure.ExternalServices;

/// <summary>
/// Reads/writes the caller's own organization (name + contact details) via
/// knightage-identity's GET/PUT /api/organizations/me, same forwarded-auth pattern
/// as IdentityUserDirectory.
/// </summary>
public class IdentityOrganizationClient : IIdentityOrganizationClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IdentityOrganizationClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OrganizationDetails> GetAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorizationHeader);
        var response = await client.GetAsync("api/organizations/me", cancellationToken);
        response.EnsureSuccessStatusCode();

        var details = await response.Content.ReadFromJsonAsync<OrganizationDetails>(cancellationToken: cancellationToken);
        return details ?? throw new InvalidOperationException("Identity returned an empty organization response.");
    }

    public async Task UpdateAsync(string name, string? contactEmail, string? contactPhone, string? address, string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorizationHeader);
        var response = await client.PutAsJsonAsync("api/organizations/me", new { name, contactEmail, contactPhone, address }, cancellationToken);

        // A 409 (name already taken) or 400 (empty / too long) carries a message meant for the
        // user. Pass it on instead of collapsing it into a generic "could not save".
        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<IdentityProblem>(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Message))
            {
                throw new OrganizationUpdateRejectedException((int)response.StatusCode, problem.Message);
            }
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed record IdentityProblem(string? Message);

    private HttpClient CreateClient(string? authorizationHeader)
    {
        var client = _httpClientFactory.CreateClient("Identity");
        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationHeader);
        }
        return client;
    }
}
