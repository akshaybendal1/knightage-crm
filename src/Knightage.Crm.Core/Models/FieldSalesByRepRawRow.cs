namespace Knightage.Crm.Core.Models;

// One row of the Sales Dashboard's "Sales by rep" chart, before display names are
// joined in -- OwnerUserId is blank (never null, see usp_Leads_GetFieldSalesByRep's
// ISNULL) for entries with no owner. SalesEntriesController.GetByRep resolves display
// names via IIdentityUserDirectory, same as GetAll already does for the entries table.
public class FieldSalesByRepRawRow
{
    public string OwnerUserId { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public decimal TotalValue { get; set; }
}
