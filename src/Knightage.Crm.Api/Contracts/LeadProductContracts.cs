using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record AddLeadProductRequest([Required] Guid ProductId, [Range(1, 100000)] int Quantity, [Required] Guid ProductStageId);

public record UpdateLeadProductQuantityRequest([Range(1, 100000)] int Quantity);

public record UpdateLeadProductStageRequest([Required] Guid ProductStageId);
