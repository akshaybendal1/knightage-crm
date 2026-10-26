using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

public record DeviceTokenRequest([Required, MaxLength(440)] string Token, [Required, MaxLength(20)] string Platform);
