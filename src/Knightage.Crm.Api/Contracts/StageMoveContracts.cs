using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

/// <summary>Most items a single bulk stage move will accept.</summary>
public static class StageMoveLimits
{
    public const int MaxItems = 100;
    public const int MaxNoteLength = 1000;
}

/// <summary>
/// One lead (or lead-product line) in a bulk move, with its own optional note. "Same note for
/// everyone" is just the client putting the same text on every item, so the API has a single
/// shape for both ways of working.
/// </summary>
public record StageMoveItem(Guid Id, [MaxLength(StageMoveLimits.MaxNoteLength)] string? Note);

public record BulkLeadStageMoveRequest([Required] Guid PipelineStageId, [Required] List<StageMoveItem> Items);

public record BulkProductStageMoveRequest([Required] Guid ProductStageId, [Required] List<StageMoveItem> Items);

/// <summary>Status is one of: Moved, Unchanged (already in that stage), NotFound, Failed.</summary>
public record StageMoveResult(Guid Id, string Status, string? Message);

public record BulkStageMoveResponse(int Moved, int Unchanged, int Failed, IReadOnlyList<StageMoveResult> Results);
