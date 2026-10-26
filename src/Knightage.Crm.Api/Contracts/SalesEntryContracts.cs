using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

// ProductStageId is optional -- when the caller omits it (e.g. an older client
// build), SalesEntriesController falls back to the org's default/Won product
// stage, same as before this field existed.
public record SalesEntryProductLine(
    [Required] Guid ProductId,
    [Range(1, int.MaxValue)] int Quantity,
    Guid? ProductStageId = null);

// CustomerId is the customer picked in the form's dropdown. When it is sent, the customer's saved name,
// company, email and phone are used for the entry (whatever the other customer fields say), and the entry's
// lead is linked to that customer. When it is omitted (an older client), the typed customer fields are used
// and, if the entry lands in a Won stage, a customer is made from them.
//
// Latitude/Longitude are required, not optional -- capturing location is the whole
// point of a field sales entry, not a best-effort nicety like it would be on a
// regular Lead. The client refuses to submit without them (see sales-entry.ts); this
// is the server-side backstop for that same rule.
public record SalesEntryRequest(
    [Required, MaxLength(200)] string CustomerName,
    [MaxLength(200)] string? Company,
    [EmailAddress, MaxLength(320)] string? Email,
    [Phone, MaxLength(50)] string? Phone,
    [MaxLength(1000)] string? Notes,
    [Required, Range(-90, 90)] decimal Latitude,
    [Required, Range(-180, 180)] decimal Longitude,
    [Required, MinLength(1)] List<SalesEntryProductLine> Products,
    Guid? CustomerId = null);
