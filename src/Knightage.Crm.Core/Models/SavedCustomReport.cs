namespace Knightage.Crm.Core.Models;

public class SavedCustomReport
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Opaque JSON (serialized CustomReportDefinition) -- replayed back into
    // CustomReportRepository.RunAsync via the same whitelisted query builder used
    // for an unsaved in-progress report. Never queried at the DB level.
    public string DefinitionJson { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
