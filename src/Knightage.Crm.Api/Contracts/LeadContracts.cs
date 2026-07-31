using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record LeadRequest(
    [property: Required] string Name,
    string? Email,
    string? Phone,
    string? Company,
    [property: Required] Guid PipelineStageId,
    string? Notes);
