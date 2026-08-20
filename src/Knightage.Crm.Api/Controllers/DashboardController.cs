using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardController(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    [HttpGet("pipeline-summary")]
    public async Task<IActionResult> GetPipelineSummary() =>
        Ok(await _dashboardRepository.GetPipelineSummaryAsync());

    [HttpGet("won-lost")]
    public async Task<IActionResult> GetWonLost([FromQuery] string range = "week")
    {
        var since = range switch
        {
            "week" => StartOfWeekUtc(DateTime.UtcNow),
            "month" => StartOfMonthUtc(DateTime.UtcNow),
            "all" => (DateTime?)null,
            _ => StartOfWeekUtc(DateTime.UtcNow)
        };

        var counts = await _dashboardRepository.GetWonLostCountsAsync(since);
        return Ok(new { counts.Won, counts.Lost, range });
    }

    [HttpGet("activity-summary")]
    public async Task<IActionResult> GetActivitySummary()
    {
        var count = await _dashboardRepository.GetActivityCountSinceAsync(StartOfWeekUtc(DateTime.UtcNow));
        return Ok(new { count });
    }

    [HttpGet("overdue-tasks")]
    public async Task<IActionResult> GetOverdueTasks()
    {
        var count = await _dashboardRepository.GetOverdueOpenTaskCountAsync(DateTime.UtcNow.Date);
        return Ok(new { count });
    }

    private static DateTime StartOfWeekUtc(DateTime utcNow)
    {
        var daysSinceMonday = ((int)utcNow.DayOfWeek + 6) % 7; // Sunday=0 -> 6, Monday=1 -> 0, ...
        return DateTime.SpecifyKind(utcNow.Date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
    }

    private static DateTime StartOfMonthUtc(DateTime utcNow) =>
        DateTime.SpecifyKind(new DateTime(utcNow.Year, utcNow.Month, 1), DateTimeKind.Utc);
}
