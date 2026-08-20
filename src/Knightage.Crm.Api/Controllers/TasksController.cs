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
    public async Task<IActionResult> GetTasks([FromQuery] string? assignedToUserId, [FromQuery] string? status)
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        var targetUserId = string.IsNullOrWhiteSpace(assignedToUserId) ? currentUserId : assignedToUserId;
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return Unauthorized();
        }

        return Ok(await _taskRepository.GetByAssigneeAsync(targetUserId, status));
    }

    [HttpPut("{id:guid}")]
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

        await _taskRepository.UpdateStatusAsync(id, request.Status, completedAtUtc);

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
