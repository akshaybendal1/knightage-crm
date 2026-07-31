namespace Knightage.Crm.Core.Models;

public class LeadImportParseResult
{
    public List<ParsedLeadLine> Leads { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
