using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record LeadActivityRequest(
    [Required, MaxLength(2000)] string Content,
    string? Type);
