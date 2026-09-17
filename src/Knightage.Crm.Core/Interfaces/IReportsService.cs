using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IReportsService
{
    Task<IReadOnlyList<LeadTrendPoint>> GetLeadTrendAsync(DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth);
    Task<IReadOnlyList<LeadSourceBreakdownItem>> GetLeadSourceBreakdownAsync(DateTime? sinceUtc, DateTime? untilUtc);
    Task<IReadOnlyList<TeamPerformanceRow>> GetTeamPerformanceAsync(DateTime? sinceUtc, DateTime? untilUtc, string? authorizationHeader);
    Task<IReadOnlyList<TaskTrendPoint>> GetTaskCompletionTrendAsync(DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth);
    Task<IReadOnlyList<LeadsByProductItem>> GetLeadsByProductAsync(DateTime? sinceUtc, DateTime? untilUtc);
    Task<IReadOnlyList<LeadsByCategoryItem>> GetLeadsByCategoryAsync(DateTime? sinceUtc, DateTime? untilUtc);
    Task<IReadOnlyList<LeadsByOwnerRow>> GetLeadsByOwnerAsync(DateTime? sinceUtc, DateTime? untilUtc, string? authorizationHeader);
    Task<IReadOnlyList<LeadSourceDetailItem>> GetLeadDetailsBySourceAsync(string source, DateTime? sinceUtc, DateTime? untilUtc);
    Task<byte[]> ExportReportAsync(string reportKey, DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth, string? authorizationHeader);
}
