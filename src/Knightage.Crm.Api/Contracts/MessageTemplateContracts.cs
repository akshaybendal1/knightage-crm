using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record MessageTemplateRequest([Required] string Name, [Required] string Body);

public record MessageTemplateStatusRequest(bool IsActive);
