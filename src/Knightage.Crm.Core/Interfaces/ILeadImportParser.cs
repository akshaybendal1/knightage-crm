using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ILeadImportParser
{
    LeadImportParseResult Parse(string csvContent);
}
