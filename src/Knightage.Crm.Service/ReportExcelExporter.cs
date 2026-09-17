using System.Globalization;
using ClosedXML.Excel;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Service;

public class ReportExcelExporter : IReportExcelExporter
{
    public byte[] BuildLeadTrend(IReadOnlyList<LeadTrendPoint> points) =>
        Build("Lead Trend", new[] { "Date", "Leads Created" }, points, p => new object[] { p.BucketStart.ToString("yyyy-MM-dd"), p.LeadCount });

    public byte[] BuildLeadSources(IReadOnlyList<LeadSourceBreakdownItem> items) =>
        Build("Lead Sources", new[] { "Source", "Lead Count" }, items, i => new object[] { i.Source, i.LeadCount });

    public byte[] BuildTeamPerformance(IReadOnlyList<TeamPerformanceRow> rows) =>
        Build("Team Performance", new[] { "Name", "Leads Created", "Tasks Completed", "Tasks Overdue" }, rows,
            r => new object[] { r.DisplayName, r.LeadsCreated, r.TasksCompleted, r.TasksOverdue });

    public byte[] BuildTaskTrend(IReadOnlyList<TaskTrendPoint> points) =>
        Build("Task Trend", new[] { "Date", "Tasks Completed" }, points, p => new object[] { p.BucketStart.ToString("yyyy-MM-dd"), p.CompletedCount });

    public byte[] BuildLeadsByProduct(IReadOnlyList<LeadsByProductItem> items) =>
        Build("Leads by Product", new[] { "Product", "Lead Count", "Total Value" }, items, i => new object[] { i.ProductName, i.LeadCount, i.TotalValue });

    public byte[] BuildLeadsByCategory(IReadOnlyList<LeadsByCategoryItem> items) =>
        Build("Leads by Category", new[] { "Category", "Lead Count", "Total Value" }, items, i => new object[] { i.Category, i.LeadCount, i.TotalValue });

    public byte[] BuildLeadsByOwner(IReadOnlyList<LeadsByOwnerRow> rows) =>
        Build("Leads by Owner", new[] { "Owner", "Lead Count" }, rows, r => new object[] { r.OwnerName, r.LeadCount });

    public byte[] BuildLeadsExport(IReadOnlyList<LeadExportRow> rows) =>
        Build("Leads", new[] { "Name", "Email", "Phone", "Company", "Stage", "Owner", "Source", "Cost" }, rows,
            r => new object[] { r.Name, r.Email ?? "", r.Phone ?? "", r.Company ?? "", r.StageName, r.OwnerName, r.Source, r.TotalCost });

    public byte[] BuildCustomReport(string[] headers, IReadOnlyList<object[]> rows) =>
        Build("Custom Report", headers, rows, r => r);

    private static byte[] Build<T>(string sheetName, string[] headers, IReadOnlyList<T> rows, Func<T, object[]> rowValues)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.SetValue(XLCellValue.FromObject(headers[col], CultureInfo.InvariantCulture));
            cell.Style.Font.Bold = true;
        }

        for (var row = 0; row < rows.Count; row++)
        {
            var values = rowValues(rows[row]);
            for (var col = 0; col < values.Length; col++)
            {
                sheet.Cell(row + 2, col + 1).SetValue(XLCellValue.FromObject(values[col], CultureInfo.InvariantCulture));
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
