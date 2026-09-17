using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ITimelineRepository
{
    Task<(IReadOnlyList<LeadTimelineItem> Items, int TotalCount)> GetLeadsTimelineAsync(Guid pipelineStageId, Guid? productId, string? ownerUserId, string? search, int page, int pageSize);
    Task<(IReadOnlyList<LeadProductTimelineItem> Items, int TotalCount)> GetLeadProductsTimelineAsync(Guid productStageId, Guid? productId, string? ownerUserId, string? search, int page, int pageSize);
}
