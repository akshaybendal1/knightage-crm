using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record LeadRequest(
    [Required, MaxLength(200)] string Name,
    [EmailAddress, MaxLength(320)] string? Email,
    [Phone, MaxLength(50)] string? Phone,
    [MaxLength(200)] string? Company,
    [Required] Guid PipelineStageId,
    [MaxLength(1000)] string? Notes,
    string? OwnerUserId);

public record UpdateLeadStageRequest([Required] Guid PipelineStageId);

public record LeadStatusRequest(bool IsActive);
