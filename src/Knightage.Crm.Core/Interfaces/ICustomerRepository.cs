using Knightage.Crm.Core.Customers;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

/// <summary>A lead sitting in a Won stage that has not been linked to a customer yet.</summary>
public sealed record WonLeadSummary(Guid Id, string Name, string? Company, string? Email, string? Phone, string? OwnerUserId);

/// <summary>A won lead that is not a customer yet, as listed on the Customers screen's "Choose leads" panel.</summary>
public sealed record WonLeadListItem(Guid Id, string Name, string? Company, string? Email, string? Phone, string? StageName, DateTime WonAtUtc);

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(Guid id);

    /// <summary>
    /// Returns the customer this person already is (same lead, email, phone, or name + company -- see
    /// usp_Customers_Upsert) or creates one. An existing customer is returned unchanged. <paramref name="leadId"/>
    /// links the lead to the customer so the same lead is never converted twice.
    /// </summary>
    Task<(Customer Customer, bool WasCreated)> UpsertAsync(CustomerDraft draft, Guid? leadId, string source, string? createdByUserId);

    Task LinkLeadAsync(Guid leadId, Guid customerId);
    Task SetInventoryLinkAsync(Guid id, Guid inventoryCustomerId);
    Task<IReadOnlyList<Customer>> GetPendingInventoryAsync(int max);
    Task<int> CountPendingInventoryAsync();
    Task<IReadOnlyList<WonLeadSummary>> GetWonLeadsWithoutCustomerAsync(int max);
    Task<int> CountWonLeadsWithoutCustomerAsync();
    Task<(IReadOnlyList<WonLeadListItem> Items, int TotalCount)> GetWonLeadsWithoutCustomerPageAsync(string? search, int page, int pageSize);
    /// <summary>The given leads that are still won and still not a customer (anything else is left out).</summary>
    Task<IReadOnlyList<WonLeadSummary>> GetWonLeadsWithoutCustomerByIdsAsync(IReadOnlyCollection<Guid> leadIds);

    /// <summary>The customer this lead is linked to, or null.</summary>
    Task<Customer?> GetForLeadAsync(Guid leadId);

    /// <summary>Removes only the lead-customer link; both records stay.</summary>
    Task UnlinkLeadAsync(Guid leadId);

    /// <summary>Type-ahead: active customers whose name, organization, email or phone contains the text.</summary>
    Task<IReadOnlyList<Customer>> SearchAsync(string? term, int max);

    /// <summary>Active customers that look like the person described (same email, phone, or name + company).</summary>
    Task<IReadOnlyList<CustomerMatch>> FindDuplicatesAsync(DuplicateQuery query, int max);
}
