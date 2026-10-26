using System.ComponentModel.DataAnnotations;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Api.Contracts;

public record LeadRequest(
    [Required, MaxLength(200)] string Name,
    [EmailAddress, MaxLength(320)] string? Email,
    [Phone, MaxLength(50)] string? Phone,
    [MaxLength(200)] string? Company,
    [Required] Guid PipelineStageId,
    [MaxLength(1000)] string? Notes,
    string? OwnerUserId);

// Note is optional: what the user wanted to record about this move (shown on the lead's timeline).
public record UpdateLeadStageRequest([Required] Guid PipelineStageId, [MaxLength(StageMoveLimits.MaxNoteLength)] string? Note = null);

public record LeadStatusRequest(bool IsActive);

// Exactly one of the two: link an existing customer (CustomerId), or make a customer from this lead's own details (CreateFromLead).
public record SetLeadCustomerRequest(Guid? CustomerId, bool CreateFromLead = false);

/// <param name="Created">A new customer record was made.</param>
/// <param name="MatchedExisting">The lead was linked to a customer that already existed (same email, phone, or name + company).</param>
public record LeadCustomerResponse(Customer? Customer, bool Created, bool MatchedExisting);
