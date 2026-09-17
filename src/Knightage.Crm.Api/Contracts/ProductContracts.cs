using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record ProductRequest([Required, MaxLength(200)] string Name, [MaxLength(1000)] string? Description, [MaxLength(100)] string? Category, [Required] decimal Price);

public record ProductStatusRequest(bool IsActive);
