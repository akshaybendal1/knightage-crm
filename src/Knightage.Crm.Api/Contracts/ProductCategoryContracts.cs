using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record ProductCategoryRequest([Required, MaxLength(100)] string Name, [Required] int SortOrder);

public record ProductCategoryStatusRequest(bool IsActive);
