using System.Net.Http.Json;
using System.Text.Json;
using Knightage.Crm.Core.Customers;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Tenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.ExternalServices;

/// <summary>
/// Sends customers to the Inventory portal (knightage-inventory-sales) so they appear there as well. The
/// portal's address and whether the organization has it come from knightage-platform
/// (GET api/tenants/me/services -- the same list the services launcher shows), so there is no extra
/// setting to configure. The caller's own token is forwarded, like every other service call from here.
///
/// The portal is a separate application that can be down, disabled for the organization, or read-only
/// (subscription), and none of that may ever get in the way of CRM work. So a failure only leaves the
/// customer "pending" -- it is retried with the next batch, and an admin can send it by hand -- and a
/// recent failure is remembered for a few minutes so a dead portal doesn't add a timeout to every request.
/// </summary>
public class InventoryCustomerSync : IInventoryCustomerSync
{
    private const string InventoryServiceKey = "InventorySales";
    private const int BatchSize = 200;
    private const int MaxBatchesPerRun = 5;
    private static readonly TimeSpan LookupCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromMinutes(3);

    private readonly ICustomerRepository _customers;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<InventoryCustomerSync> _logger;

    public InventoryCustomerSync(
        ICustomerRepository customers,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        TenantContext tenantContext,
        ILogger<InventoryCustomerSync> logger)
    {
        _customers = customers;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private sealed record Target(string? BaseUrl);
    private sealed record SyncItem(Guid CrmCustomerId, Guid InventoryCustomerId);
    private sealed record SyncResponse(List<SyncItem>? Items);

    private static string TargetKey(Guid organizationId) => $"inventory-target:{organizationId}";
    private static string BackoffKey(Guid organizationId) => $"inventory-backoff:{organizationId}";
    private static string PullKey(Guid organizationId) => $"inventory-pull:{organizationId}";

    private static readonly TimeSpan PullInterval = TimeSpan.FromMinutes(2);

    private sealed record InventoryCustomer(Guid Id, string Name, string? Email, string? Phone, bool IsActive);

    public async Task<InventorySyncResult> PushPendingAsync(string? authorizationHeader, bool force, CancellationToken cancellationToken)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var pending = await _customers.CountPendingInventoryAsync();
        if (pending == 0)
        {
            return new InventorySyncResult(true, 0, 0, null);
        }

        if (!force && _cache.TryGetValue(BackoffKey(organizationId), out _))
        {
            return new InventorySyncResult(false, 0, pending, "The Inventory portal didn't answer a moment ago; customers will be sent on the next try.");
        }

        try
        {
            var baseUrl = (await GetTargetAsync(organizationId, authorizationHeader, cancellationToken)).BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return new InventorySyncResult(false, 0, pending, "The Inventory portal isn't enabled for this organization.");
            }

            var pushed = 0;
            for (var batch = 0; batch < MaxBatchesPerRun; batch++)
            {
                var customers = await _customers.GetPendingInventoryAsync(BatchSize);
                if (customers.Count == 0)
                {
                    break;
                }

                var items = await PostBatchAsync(baseUrl, authorizationHeader, customers, cancellationToken);
                var known = customers.Select(c => c.Id).ToHashSet();
                foreach (var item in items.Where(i => known.Contains(i.CrmCustomerId)))
                {
                    await _customers.SetInventoryLinkAsync(item.CrmCustomerId, item.InventoryCustomerId);
                    pushed++;
                }

                // Nothing was accepted from a non-empty batch: stop instead of asking for the same rows again.
                if (items.Count == 0)
                {
                    break;
                }
            }

            _cache.Remove(BackoffKey(organizationId));
            var stillPending = await _customers.CountPendingInventoryAsync();
            return new InventorySyncResult(true, pushed, stillPending, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Could not send customers to the Inventory portal for organization {OrganizationId}.", organizationId);
            _cache.Set(BackoffKey(organizationId), true, FailureBackoff);
            return new InventorySyncResult(false, 0, pending, "Could not reach the Inventory portal. The customers are saved here and will be sent later.");
        }
    }

    public async Task<int> PullNewAsync(string? authorizationHeader, CancellationToken cancellationToken)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        // One attempt per organization per interval, success or failure: this runs on every customer list
        // load, and the portal's list does not need to be fresher than that.
        if (_cache.TryGetValue(PullKey(organizationId), out _) || _cache.TryGetValue(BackoffKey(organizationId), out _))
        {
            return 0;
        }
        _cache.Set(PullKey(organizationId), true, PullInterval);

        try
        {
            var baseUrl = (await GetTargetAsync(organizationId, authorizationHeader, cancellationToken)).BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return 0;
            }

            var client = _httpClientFactory.CreateClient("InventorySales");
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "api/customers"));
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var inventoryCustomers = await response.Content.ReadFromJsonAsync<List<InventoryCustomer>>(cancellationToken: cancellationToken) ?? new();

            var crmCustomers = await _customers.GetAllAsync();
            var alreadyLinked = crmCustomers.Where(c => c.InventoryCustomerId is not null).Select(c => c.InventoryCustomerId!.Value).ToHashSet();

            var changed = 0;
            foreach (var item in inventoryCustomers.Where(c => c.IsActive && !alreadyLinked.Contains(c.Id)))
            {
                var draft = CustomerDraft.Create(item.Name, null, item.Email, item.Phone);
                if (draft is null)
                {
                    continue;
                }

                // Same duplicate check as everywhere else: someone already in CRM (same email/phone) is linked,
                // not added twice. A CRM customer already linked to a different Inventory record is left alone.
                var (customer, _) = await _customers.UpsertAsync(draft, null, Core.Models.CustomerSources.Inventory, null);
                if (customer.InventoryCustomerId is null)
                {
                    await _customers.SetInventoryLinkAsync(customer.Id, item.Id);
                    alreadyLinked.Add(item.Id);
                    changed++;
                }
            }

            return changed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Could not read customers from the Inventory portal for organization {OrganizationId}.", organizationId);
            _cache.Set(BackoffKey(organizationId), true, FailureBackoff);
            return 0;
        }
    }

    private async Task<Target> GetTargetAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(TargetKey(organizationId), out Target? cached) && cached is not null)
        {
            return cached;
        }

        var client = _httpClientFactory.CreateClient("Platform");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/tenants/me/services");
        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        string? url = null;
        if (document.RootElement.TryGetProperty("services", out var services) && services.ValueKind == JsonValueKind.Array)
        {
            foreach (var service in services.EnumerateArray())
            {
                var key = service.TryGetProperty("key", out var k) ? k.GetString() : null;
                if (!string.Equals(key, InventoryServiceKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var provisioned = service.TryGetProperty("provisioned", out var p) && p.ValueKind == JsonValueKind.True;
                url = provisioned && service.TryGetProperty("url", out var u) ? u.GetString() : null;
            }
        }

        var target = new Target(url);
        _cache.Set(TargetKey(organizationId), target, LookupCacheDuration);
        return target;
    }

    private async Task<List<SyncItem>> PostBatchAsync(string baseUrl, string? authorizationHeader, IReadOnlyList<Core.Models.Customer> customers, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("InventorySales");
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "api/customers/crm-sync"))
        {
            Content = JsonContent.Create(new
            {
                customers = customers.Select(c => new { crmCustomerId = c.Id, name = c.Name, email = c.Email, phone = c.Phone, phoneKey = CustomerDraft.PhoneKeyOf(c.Phone) }),
            }),
        };
        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SyncResponse>(cancellationToken: cancellationToken);
        return body?.Items ?? new List<SyncItem>();
    }
}
