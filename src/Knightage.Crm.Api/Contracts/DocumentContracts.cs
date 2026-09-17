using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record CreateDocumentRequest([Required] string Type, [Range(0, 100)] decimal TaxRatePercent = 0, [Range(0, 100)] decimal DiscountPercent = 0);
