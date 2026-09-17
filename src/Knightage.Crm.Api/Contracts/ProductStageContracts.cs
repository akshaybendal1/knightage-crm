using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record ProductStageRequest(
    [Required] string Name,
    [Required] int SortOrder,
    bool IsWon = false,
    bool IsLost = false,
    bool IsDefault = false);

public record ProductStageStatusRequest(bool IsActive);
