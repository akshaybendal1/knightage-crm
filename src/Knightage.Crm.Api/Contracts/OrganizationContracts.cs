using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record UpdateOrganizationRequest(
    [Required] string Name,
    [EmailAddress] string? ContactEmail,
    string? ContactPhone,
    string? Address);
