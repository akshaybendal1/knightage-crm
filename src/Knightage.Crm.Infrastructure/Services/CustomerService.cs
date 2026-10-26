using Knightage.Crm.Core.Customers;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    // One import run handles at most this many won leads, in batches, so a very large organization
    // never holds a request open for minutes; "Remaining" tells the admin to run it again.
    private const int ImportCapPerRun = 2000;
    private const int ImportBatchSize = 500;

    private readonly ICustomerRepository _customers;
    private readonly IInventoryCustomerSync _inventorySync;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerRepository customers, IInventoryCustomerSync inventorySync, ILogger<CustomerService> logger)
    {
        _customers = customers;
        _inventorySync = inventorySync;
        _logger = logger;
    }

    public async Task ConvertWonLeadsAsync(IReadOnlyCollection<Lead> leads, string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken)
    {
        var created = 0;
        foreach (var lead in leads)
        {
            try
            {
                var draft = CustomerDraft.FromLead(lead);
                if (draft is null)
                {
                    continue;
                }

                var (_, wasCreated) = await _customers.UpsertAsync(draft, lead.Id, CustomerSources.WonLead, createdByUserId);
                if (wasCreated)
                {
                    created++;
                }
            }
            catch (Exception ex)
            {
                // The lead is already won; the customer can still be created later from "Import from won leads".
                _logger.LogError(ex, "Could not convert won lead {LeadId} into a customer.", lead.Id);
            }
        }

        if (created > 0)
        {
            await TryPushToInventoryAsync(authorizationHeader, cancellationToken);
        }
    }

    public async Task<CustomerSaveResult> AddAsync(CustomerDraft draft, string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken)
    {
        var (customer, wasCreated) = await _customers.UpsertAsync(draft, null, CustomerSources.Manual, createdByUserId);
        if (wasCreated)
        {
            await TryPushToInventoryAsync(authorizationHeader, cancellationToken);
            // Re-read so the answer already carries the Inventory id the push just stored.
            customer = await _customers.GetByIdAsync(customer.Id) ?? customer;
        }

        return new CustomerSaveResult(customer, !wasCreated);
    }

    public async Task<WonLeadImportResult> ImportWonLeadsAsync(string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken)
    {
        var imported = 0;
        var linked = 0;
        var handled = 0;
        var failedIds = new HashSet<Guid>();

        while (handled < ImportCapPerRun)
        {
            var batch = await _customers.GetWonLeadsWithoutCustomerAsync(ImportBatchSize + failedIds.Count);
            var todo = batch.Where(l => !failedIds.Contains(l.Id)).Take(Math.Min(ImportBatchSize, ImportCapPerRun - handled)).ToList();
            if (todo.Count == 0)
            {
                break;
            }

            foreach (var lead in todo)
            {
                handled++;
                try
                {
                    var draft = CustomerDraft.Create(lead.Name, lead.Company, lead.Email, lead.Phone);
                    if (draft is null)
                    {
                        failedIds.Add(lead.Id);
                        continue;
                    }

                    var (_, wasCreated) = await _customers.UpsertAsync(draft, lead.Id, CustomerSources.WonLead, createdByUserId);
                    if (wasCreated) imported++; else linked++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not import won lead {LeadId} as a customer.", lead.Id);
                    failedIds.Add(lead.Id);
                }
            }
        }

        var inventory = imported > 0
            ? await TryPushToInventoryAsync(authorizationHeader, cancellationToken)
            : new InventorySyncResult(true, 0, await _customers.CountPendingInventoryAsync(), null);
        var remaining = await _customers.CountWonLeadsWithoutCustomerAsync();
        return new WonLeadImportResult(imported, linked, remaining, inventory);
    }

    public async Task<WonLeadImportResult> ConvertSelectedWonLeadsAsync(IReadOnlyCollection<Guid> leadIds, string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken)
    {
        // Re-checked on the server: ids that are not this organization's, not won any more, or already a
        // customer are simply not returned, so a stale selection can never convert the wrong lead.
        var leads = await _customers.GetWonLeadsWithoutCustomerByIdsAsync(leadIds.Distinct().ToList());
        var imported = 0;
        var linked = 0;
        foreach (var lead in leads)
        {
            try
            {
                var draft = CustomerDraft.Create(lead.Name, lead.Company, lead.Email, lead.Phone);
                if (draft is null)
                {
                    continue;
                }

                var (_, wasCreated) = await _customers.UpsertAsync(draft, lead.Id, CustomerSources.WonLead, createdByUserId);
                if (wasCreated) imported++; else linked++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not convert selected won lead {LeadId} into a customer.", lead.Id);
            }
        }

        var inventory = imported > 0
            ? await TryPushToInventoryAsync(authorizationHeader, cancellationToken)
            : new InventorySyncResult(true, 0, await _customers.CountPendingInventoryAsync(), null);
        var remaining = await _customers.CountWonLeadsWithoutCustomerAsync();
        return new WonLeadImportResult(imported, linked, remaining, inventory);
    }

    public async Task<CustomerSaveResult?> CreateFromLeadAsync(Lead lead, string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken)
    {
        var draft = CustomerDraft.FromLead(lead);
        if (draft is null)
        {
            return null;
        }

        var (customer, wasCreated) = await _customers.UpsertAsync(draft, lead.Id, CustomerSources.Lead, createdByUserId);
        if (wasCreated)
        {
            await TryPushToInventoryAsync(authorizationHeader, cancellationToken);
            customer = await _customers.GetByIdAsync(customer.Id) ?? customer;
        }

        return new CustomerSaveResult(customer, !wasCreated);
    }

    private async Task<InventorySyncResult> TryPushToInventoryAsync(string? authorizationHeader, CancellationToken cancellationToken)
    {
        try
        {
            return await _inventorySync.PushPendingAsync(authorizationHeader, force: false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sending customers to the Inventory portal failed.");
            return new InventorySyncResult(false, 0, 0, "Could not reach the Inventory portal.");
        }
    }
}
