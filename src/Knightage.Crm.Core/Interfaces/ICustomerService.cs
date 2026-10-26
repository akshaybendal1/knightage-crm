using Knightage.Crm.Core.Customers;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public sealed record CustomerSaveResult(Customer Customer, bool AlreadyExisted);

/// <param name="Imported">Customers newly created.</param>
/// <param name="LinkedToExisting">Won leads that turned out to be an existing customer and were linked to it.</param>
/// <param name="Remaining">Won leads still without a customer (more than one run's cap, or ones that failed).</param>
public sealed record WonLeadImportResult(int Imported, int LinkedToExisting, int Remaining, InventorySyncResult Inventory);

/// <param name="Available">False when the Inventory portal is not enabled for the organization or could not be reached.</param>
/// <param name="Pushed">Customers sent to Inventory in this run.</param>
/// <param name="Pending">Customers still waiting to be sent.</param>
public sealed record InventorySyncResult(bool Available, int Pushed, int Pending, string? Message);

/// <summary>
/// Turns won leads into customers and keeps the Inventory portal's customer list in step. Every method that
/// runs as a side effect of something else (a stage move, a field sale) swallows its own failures -- losing
/// a customer must never fail the move or the sale that triggered it; the next import picks it up.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Makes a customer for each lead (or links it to the one it already is). Call after the lead has reached a
    /// Won stage. Never throws; sends new customers to Inventory once at the end, not once per lead.
    /// </summary>
    Task ConvertWonLeadsAsync(IReadOnlyCollection<Lead> leads, string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken);

    /// <summary>Adds a customer from a form. Returns the existing one (AlreadyExisted) when the same person is already saved.</summary>
    Task<CustomerSaveResult> AddAsync(CustomerDraft draft, string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken);

    /// <summary>Turns leads that are already in a Won stage into customers, up to a cap per run.</summary>
    Task<WonLeadImportResult> ImportWonLeadsAsync(string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken);

    /// <summary>
    /// Turns only the chosen won leads into customers (the "Choose leads" panel). Leads that are not won, already a
    /// customer, or not this organization's are skipped. New customers are sent to Inventory once at the end.
    /// </summary>
    Task<WonLeadImportResult> ConvertSelectedWonLeadsAsync(IReadOnlyCollection<Guid> leadIds, string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken);

    /// <summary>
    /// Makes this lead a customer right now (it does not have to be won): returns the customer this person already is
    /// (AlreadyExisted) or creates one, and links the lead to it. Null when the lead has no usable name.
    /// </summary>
    Task<CustomerSaveResult?> CreateFromLeadAsync(Lead lead, string? createdByUserId, string? authorizationHeader, CancellationToken cancellationToken);
}

public interface IInventoryCustomerSync
{
    /// <summary>
    /// Sends customers that are not in the Inventory portal yet (up to 200). When <paramref name="force"/> is false a
    /// recent failure is respected (skipped for a few minutes) so a down portal does not slow every request;
    /// an admin pressing "send now" passes true.
    /// </summary>
    Task<InventorySyncResult> PushPendingAsync(string? authorizationHeader, bool force, CancellationToken cancellationToken);

    /// <summary>
    /// Brings customers added in the Inventory portal into CRM (or links them to the CRM customer they already
    /// are), so they show up in the field sales customer dropdown. Runs at most every couple of minutes per
    /// organization and never throws; returns how many CRM customers were added or linked.
    /// </summary>
    Task<int> PullNewAsync(string? authorizationHeader, CancellationToken cancellationToken);
}
