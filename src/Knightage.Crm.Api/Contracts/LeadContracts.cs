using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record LeadRequest(
    [Required] string Name,
    string? Email,
    string? Phone,
    string? Company,
    [Required] Guid PipelineStageId,
    string? Notes);
