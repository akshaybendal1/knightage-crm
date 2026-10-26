namespace Knightage.Crm.Core.Models;

/// <summary>
/// A person or company that has bought (a lead that reached a Won stage) or that a rep added by hand.
/// One customer can come from several leads (a repeat buyer) -- the link is the CustomerLeads table.
/// </summary>
public class Customer
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>"WonLead" (converted from a won lead) or "Manual" (added from a form).</summary>
    public string Source { get; set; } = CustomerSources.Manual;

    public bool IsActive { get; set; } = true;

    /// <summary>Id of the matching customer in the Inventory portal; null until it has been sent there.</summary>
    public Guid? InventoryCustomerId { get; set; }
    public DateTime? InventorySyncedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Number of leads linked to this customer; filled in by the list query only.</summary>
    public int LeadCount { get; set; }
}

public static class CustomerSources
{
    public const string WonLead = "WonLead";
    public const string Manual = "Manual";
    /// <summary>Made from a lead directly ("Add as customer"), before or without the lead being won.</summary>
    public const string Lead = "Lead";
    /// <summary>Added in the Inventory portal and brought over by IInventoryCustomerSync.PullNewAsync.</summary>
    public const string Inventory = "Inventory";
}
