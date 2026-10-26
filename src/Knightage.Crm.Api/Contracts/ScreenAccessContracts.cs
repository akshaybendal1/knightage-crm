using System.ComponentModel.DataAnnotations;

namespace Knightage.Crm.Api.Contracts;

/// <summary>
/// The desired access for each configurable role: role -> (screen key -> allowed). Cells that are
/// not listed use the built-in default, so the Access Control screen always sends the complete set.
/// </summary>
public record SaveScreenAccessRequest([Required] Dictionary<string, Dictionary<string, bool>> Allowed);
