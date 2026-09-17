using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

// Wire shape for both "run" (preview) and "save" -- CustomReportsController validates
// Dimensions/ViewType against the whitelist before this ever reaches the repository.
public record CustomReportDefinition(
    [Required] List<string> Dimensions,
    [Required] string ViewType,
    [Required] string Range,
    DateTime? From,
    DateTime? To,
    List<Guid>? StageIds,
    List<string>? OwnerUserIds,
    List<Guid>? ProductIds,
    List<string>? Sources);

public record SaveCustomReportRequest([Required] string Name, [Required] CustomReportDefinition Definition);

public record CustomReportStatusRequest(bool IsActive);
