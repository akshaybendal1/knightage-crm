using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

// No role restriction -- visible to every role, matching the Dashboard's stuck-leads
// count being visible to every role rather than admin-only.
[Authorize]
[ApiController]
[Route("api/timeline")]
public class TimelineController : ControllerBase
{
    private readonly ITimelineRepository _timelineRepository;

    public TimelineController(ITimelineRepository timelineRepository)
    {
        _timelineRepository = timelineRepository;
    }

    [HttpGet("leads")]
    public async Task<IActionResult> GetLeadsTimeline([FromQuery] Guid pipelineStageId, [FromQuery] Guid? productId, [FromQuery] string? ownerUserId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 15 : pageSize;

        var (items, totalCount) = await _timelineRepository.GetLeadsTimelineAsync(pipelineStageId, productId, ownerUserId, search, page, pageSize);
        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.DaysInStage = (int)(now - item.StageChangedAtUtc).TotalDays;
        }
        return Ok(new { items, totalCount });
    }

    [HttpGet("lead-products")]
    public async Task<IActionResult> GetLeadProductsTimeline([FromQuery] Guid productStageId, [FromQuery] Guid? productId, [FromQuery] string? ownerUserId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 15 : pageSize;

        var (items, totalCount) = await _timelineRepository.GetLeadProductsTimelineAsync(productStageId, productId, ownerUserId, search, page, pageSize);
        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.DaysInStatus = (int)(now - item.StatusChangedAtUtc).TotalDays;
        }
        return Ok(new { items, totalCount });
    }
}
