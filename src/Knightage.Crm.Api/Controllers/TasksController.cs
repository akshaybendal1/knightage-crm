using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ILeadTaskRepository _taskRepository;
    private readonly ILeadActivityRepository _activityRepository;

    public TasksController(ILeadTaskRepository taskRepository, ILeadActivityRepository activityRepository)
    {
        _taskRepository = taskRepository;
        _activityRepository = activityRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] string? assignedToUserId,
        [FromQuery] string? status,
        [FromQuery] bool all = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        // "all" is the manager/dashboard drill-down view (every user's tasks in this
        // tenant) -- there's no roles/permissions model yet to gate this behind, same
        // as every other endpoint in this service.
        if (all)
        {
            var (allItems, allTotal) = await _taskRepository.GetByAssigneePagedAsync(null, status, page, pageSize);
            return Ok(new { items = allItems, totalCount = allTotal });
        }

        var currentUserId = User.FindFirst("sub")?.Value;
        var targetUserId = string.IsNullOrWhiteSpace(assignedToUserId) ? currentUserId : assignedToUserId;
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return Unauthorized();
        }

        var (items, totalCount) = await _taskRepository.GetByAssigneePagedAsync(targetUserId, status, page, pageSize);
        return Ok(new { items, totalCount });
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendarTasks([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] bool all = false)
    {
        // Same "all" semantics/no-role-gate as GetTasks above -- frontend gates the
        // whole-team toggle to Owner/Admin, consistent with the rest of this API.
        if (all)
        {
            var allItems = await _taskRepository.GetByDateRangeAsync(null, from.Date, to.Date);
            return Ok(allItems);
        }

        var currentUserId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized();
        }

        var items = await _taskRepository.GetByDateRangeAsync(currentUserId, from.Date, to.Date);
        return Ok(items);
    }

    [HttpGet("due-count")]
    public async Task<IActionResult> GetDueCount()
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized();
        }

        var count = await _taskRepository.GetDueOrOverdueCountAsync(currentUserId, DateTime.UtcNow.Date);
        return Ok(new { count });
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task is null)
        {
            return NotFound();
        }

        if (request.Status is not ("Open" or "Completed"))
        {
            return BadRequest(new { message = "Status must be 'Open' or 'Completed'." });
        }

        var wasCompleted = task.Status == "Completed";
        var isNowCompleted = request.Status == "Completed";
        DateTime? completedAtUtc = isNowCompleted ? (task.CompletedAtUtc ?? DateTime.UtcNow) : null;
        var now = DateTime.UtcNow;

        await _taskRepository.UpdateStatusAsync(id, request.Status, completedAtUtc, User.FindFirst("sub")?.Value, now);

        if (isNowCompleted && !wasCompleted)
        {
            await _activityRepository.CreateAsync(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = task.LeadId,
                Type = "TaskCompleted",
                Content = $"Completed task: {task.Title}",
                CreatedByUserId = User.FindFirst("sub")?.Value,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return Ok(await _taskRepository.GetByIdAsync(id));
    }
}
