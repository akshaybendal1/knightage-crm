using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IReportsRepository
{
    Task<IReadOnlyList<LeadTrendPoint>> GetLeadTrendAsync(DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth);
    Task<IReadOnlyList<LeadSourceBreakdownItem>> GetLeadSourceBreakdownAsync(DateTime? sinceUtc, DateTime? untilUtc);
    Task<TeamRawCounts> GetTeamRawCountsAsync(DateTime? sinceUtc, DateTime? untilUtc, DateTime todayUtc);
    Task<IReadOnlyList<TaskTrendPoint>> GetTaskCompletionTrendAsync(DateTime? sinceUtc, DateTime? untilUtc, bool bucketByMonth);
    Task<IReadOnlyList<LeadsByProductItem>> GetLeadsByProductAsync(DateTime? sinceUtc, DateTime? untilUtc);
    Task<IReadOnlyList<LeadsByCategoryItem>> GetLeadsByCategoryAsync(DateTime? sinceUtc, DateTime? untilUtc);
    Task<IReadOnlyList<UserCount>> GetLeadsByOwnerRawAsync(DateTime? sinceUtc, DateTime? untilUtc);
    Task<IReadOnlyList<LeadSourceDetailItem>> GetLeadDetailsBySourceAsync(string source, DateTime? sinceUtc, DateTime? untilUtc);
}
