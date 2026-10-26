using System.ComponentModel.DataAnnotations;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Api.Contracts;

public record CustomerRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(200)] string? Company,
    [EmailAddress, MaxLength(320)] string? Email,
    [Phone, MaxLength(50)] string? Phone);

/// <param name="AlreadyExisted">True when the same person was already saved, so nothing new was created.</param>
public record CustomerSaveResponse(Customer Customer, bool AlreadyExisted);

/// <summary>The won leads ticked on the Customers screen's "Choose leads" panel (at most 500 per request).</summary>
public record ConvertWonLeadsRequest(
    [Required, MinLength(1, ErrorMessage = "Choose at least one lead."), MaxLength(500, ErrorMessage = "Convert at most 500 leads at a time.")] List<Guid> LeadIds);
