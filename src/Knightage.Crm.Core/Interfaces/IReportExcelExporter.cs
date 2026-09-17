using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IReportExcelExporter
{
    byte[] BuildLeadTrend(IReadOnlyList<LeadTrendPoint> points);
    byte[] BuildLeadSources(IReadOnlyList<LeadSourceBreakdownItem> items);
    byte[] BuildTeamPerformance(IReadOnlyList<TeamPerformanceRow> rows);
    byte[] BuildTaskTrend(IReadOnlyList<TaskTrendPoint> points);
    byte[] BuildLeadsByProduct(IReadOnlyList<LeadsByProductItem> items);
    byte[] BuildLeadsByCategory(IReadOnlyList<LeadsByCategoryItem> items);
    byte[] BuildLeadsByOwner(IReadOnlyList<LeadsByOwnerRow> rows);
    byte[] BuildLeadsExport(IReadOnlyList<LeadExportRow> rows);

    // Unlike every other Build* method, a custom report's columns aren't known at
    // compile time (the user picks them) -- rows are pre-projected to object[] by the
    // caller instead of a typed T.
    byte[] BuildCustomReport(string[] headers, IReadOnlyList<object[]> rows);
}
