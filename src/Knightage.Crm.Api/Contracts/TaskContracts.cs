using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record CreateTaskRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    [Required] DateTime DueDate,
    string? AssignedToUserId);

public record UpdateTaskStatusRequest([Required] string Status);
