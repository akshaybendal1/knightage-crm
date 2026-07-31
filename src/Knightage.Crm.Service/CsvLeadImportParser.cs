using System.Text;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Service;

/// <summary>
/// Expects a simple "Name,Email,Phone,Company" CSV (optional header row). Only Name is
/// required per row. Quoted fields with embedded commas are supported. Malformed rows are
/// skipped and reported rather than failing the whole import, same approach as
/// knightage-accounting's CsvBankStatementParser.
/// </summary>
public class CsvLeadImportParser : ILeadImportParser
{
    public LeadImportParseResult Parse(string csvContent)
    {
        var result = new LeadImportParseResult();
        var lines = csvContent.Replace("\r\n", "\n").Split('\n');

        var startIndex = 0;
        if (lines.Length > 0 && lines[0].TrimStart().StartsWith("Name", StringComparison.OrdinalIgnoreCase))
        {
            startIndex = 1;
        }

        for (var i = startIndex; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var lineNumber = i + 1;
            var fields = ParseCsvLine(rawLine);

            if (fields.Count < 1 || string.IsNullOrWhiteSpace(fields[0]))
            {
                result.Errors.Add($"Line {lineNumber}: Name is required.");
                continue;
            }

            result.Leads.Add(new ParsedLeadLine
            {
                Name = fields[0].Trim(),
                Email = fields.Count > 1 && !string.IsNullOrWhiteSpace(fields[1]) ? fields[1].Trim() : null,
                Phone = fields.Count > 2 && !string.IsNullOrWhiteSpace(fields[2]) ? fields[2].Trim() : null,
                Company = fields.Count > 3 && !string.IsNullOrWhiteSpace(fields[3]) ? fields[3].Trim() : null
            });
        }

        return result;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
