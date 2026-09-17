using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize(Roles = "Owner,Admin")]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet("lead-trends")]
    public async Task<IActionResult> GetLeadTrends([FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (since, until, bucketByMonth) = ResolveRange(range, from, to);
        return Ok(await _reportsService.GetLeadTrendAsync(since, until, bucketByMonth));
    }

    [HttpGet("lead-sources")]
    public async Task<IActionResult> GetLeadSources([FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (since, until, _) = ResolveRange(range, from, to);
        return Ok(await _reportsService.GetLeadSourceBreakdownAsync(since, until));
    }

    [HttpGet("team-performance")]
    public async Task<IActionResult> GetTeamPerformance([FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (since, until, _) = ResolveRange(range, from, to);
        var authorizationHeader = Request.Headers.Authorization.ToString();
        return Ok(await _reportsService.GetTeamPerformanceAsync(since, until, authorizationHeader));
    }

    [HttpGet("task-trends")]
    public async Task<IActionResult> GetTaskTrends([FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (since, until, bucketByMonth) = ResolveRange(range, from, to);
        return Ok(await _reportsService.GetTaskCompletionTrendAsync(since, until, bucketByMonth));
    }

    [HttpGet("leads-by-product")]
    public async Task<IActionResult> GetLeadsByProduct([FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (since, until, _) = ResolveRange(range, from, to);
        return Ok(await _reportsService.GetLeadsByProductAsync(since, until));
    }

    [HttpGet("leads-by-category")]
    public async Task<IActionResult> GetLeadsByCategory([FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (since, until, _) = ResolveRange(range, from, to);
        return Ok(await _reportsService.GetLeadsByCategoryAsync(since, until));
    }

    [HttpGet("leads-by-owner")]
    public async Task<IActionResult> GetLeadsByOwner([FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (since, until, _) = ResolveRange(range, from, to);
        var authorizationHeader = Request.Headers.Authorization.ToString();
        return Ok(await _reportsService.GetLeadsByOwnerAsync(since, until, authorizationHeader));
    }

    // Drill-down from the Lead source breakdown chart: the actual leads behind one
    // source's count, not just the number.
    [HttpGet("lead-sources/detail")]
    public async Task<IActionResult> GetLeadSourceDetail([FromQuery] string source, [FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return BadRequest(new { message = "source is required." });
        }

        var (since, until, _) = ResolveRange(range, from, to);
        return Ok(await _reportsService.GetLeadDetailsBySourceAsync(source, since, until));
    }

    // Keys here must stay in sync with the switch in ReportsService.ExportReportAsync --
    // doubles as request validation, so an unknown key returns a clean 400 instead of
    // hitting the service's defensive exception.
    private static readonly Dictionary<string, string> ReportFileNames = new()
    {
        ["lead-trends"] = "lead-trends",
        ["lead-sources"] = "lead-source-breakdown",
        ["team-performance"] = "team-performance",
        ["task-trends"] = "task-completion-trend",
        ["leads-by-product"] = "leads-by-product",
        ["leads-by-category"] = "leads-by-category",
        ["leads-by-owner"] = "leads-by-owner",
    };

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string report, [FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!ReportFileNames.TryGetValue(report, out var filePrefix))
        {
            return BadRequest(new { message = "Unknown report key." });
        }

        var (since, until, bucketByMonth) = ResolveRange(range, from, to);
        var authorizationHeader = Request.Headers.Authorization.ToString();
        var content = await _reportsService.ExportReportAsync(report, since, until, bucketByMonth, authorizationHeader);
        var fileName = $"{filePrefix}-{range}-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // "all" (and any custom range longer than ~2 months) buckets by month instead of day so
    // a mature org's trend chart doesn't end up with hundreds of daily points.
    //
    // Until is an EXCLUSIVE upper bound (the day after "to", at midnight) so a custom range
    // of Sep 1-Sep 5 inclusive captures all of Sep 5, not just up to midnight at its start.
    //
    // internal (not private): CustomReportsController reuses this exact convention rather
    // than duplicating it.
    internal static (DateTime? Since, DateTime? Until, bool BucketByMonth) ResolveRange(string range, DateTime? from, DateTime? to)
    {
        if (range == "custom" && from.HasValue && to.HasValue)
        {
            var since = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            var until = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            var bucketByMonth = (until - since).TotalDays > 60;
            return (since, until, bucketByMonth);
        }

        var resolvedSince = range switch
        {
            "week" => StartOfWeekUtc(DateTime.UtcNow),
            "month" => StartOfMonthUtc(DateTime.UtcNow),
            "all" => (DateTime?)null,
            _ => StartOfWeekUtc(DateTime.UtcNow)
        };
        return (resolvedSince, null, range == "all");
    }

    private static DateTime StartOfWeekUtc(DateTime utcNow)
    {
        var daysSinceMonday = ((int)utcNow.DayOfWeek + 6) % 7; // Sunday=0 -> 6, Monday=1 -> 0, ...
        return DateTime.SpecifyKind(utcNow.Date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
    }

    private static DateTime StartOfMonthUtc(DateTime utcNow) =>
        DateTime.SpecifyKind(new DateTime(utcNow.Year, utcNow.Month, 1), DateTimeKind.Utc);
}
