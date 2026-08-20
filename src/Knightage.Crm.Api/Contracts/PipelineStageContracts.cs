using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record PipelineStageRequest(
    [Required] string Name,
    [Required] int SortOrder,
    bool IsWon = false,
    bool IsLost = false);
