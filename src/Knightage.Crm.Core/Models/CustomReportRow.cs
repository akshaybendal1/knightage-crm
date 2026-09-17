namespace Knightage.Crm.Core.Models;

// One grouped bucket from a custom report run. Dim1/Dim2Value are the raw grouped
// value (a stage/product id, a user id, the Source string, or an ISO date) as
// returned by CustomReportRepository.RunAsync; the matching *Label fields are filled
// in afterward by CustomReportsController (owner/stage/product need a directory
// lookup, source/createdDate use the raw value directly) -- mirrors how
// ReportsService.GetLeadsByOwnerAsync resolves owner names after its own query runs.
public class CustomReportRow
{
    public string Dim1Value { get; set; } = string.Empty;
    public string Dim1Label { get; set; } = string.Empty;
    public string? Dim2Value { get; set; }
    public string? Dim2Label { get; set; }
    public int LeadCount { get; set; }
    public decimal TotalValue { get; set; }
}
