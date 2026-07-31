using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record PipelineStageRequest(
    [property: Required] string Name,
    [property: Required] int SortOrder);
