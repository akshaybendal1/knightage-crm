using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record CreateMeetingRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    [Required] DateTime StartAtUtc,
    [Required] DateTime EndAtUtc,
    string? Location,
    string? OwnerUserId);

public record UpdateMeetingRequest(
    [Required, MaxLength(200)] string Title,
    string? Description,
    [Required] DateTime StartAtUtc,
    [Required] DateTime EndAtUtc,
    string? Location);

public record UpdateMeetingStatusRequest([Required] string Status);
