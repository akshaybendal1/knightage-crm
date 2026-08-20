using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IDashboardRepository
{
    Task<IReadOnlyList<PipelineSummaryItem>> GetPipelineSummaryAsync();
    Task<WonLostCounts> GetWonLostCountsAsync(DateTime? sinceUtc);
    Task<int> GetActivityCountSinceAsync(DateTime sinceUtc);
    Task<int> GetOverdueOpenTaskCountAsync(DateTime todayUtc);
}
