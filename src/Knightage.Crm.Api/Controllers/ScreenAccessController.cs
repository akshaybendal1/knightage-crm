using System.Security.Claims;
using Knightage.Crm.Api.Authorization;
using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.ScreenAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

// Which screens each role can open. "me" is for every signed-in user (it drives their menu and route
// guards); the matrix and its save are for the Owner and Admins (the locked "access-control" screen).
[Authorize]
[ApiController]
[Route("api/screen-access")]
public class ScreenAccessController : ControllerBase
{
    private readonly IScreenAccessService _screenAccessService;

    public ScreenAccessController(IScreenAccessService screenAccessService)
    {
        _screenAccessService = screenAccessService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var screens = await _screenAccessService.GetAllowedScreensAsync(role);
        return Ok(new { role, screens });
    }

    [RequireScreen("access-control")]
    [HttpGet]
    public async Task<IActionResult> GetMatrix()
    {
        var matrix = await _screenAccessService.GetMatrixAsync();
        var screens = ScreenCatalog.All.Select(s => new
        {
            s.Key,
            s.Label,
            s.Group,
            Kind = s.Kind.ToString(),
            Defaults = ScreenCatalog.ConfigurableRoles.ToDictionary(r => r, r => ScreenCatalog.DefaultFor(r, s)),
            Allowed = ScreenCatalog.ConfigurableRoles.ToDictionary(r => r, r => matrix.AllowedByRole[r][s.Key]),
        });
        return Ok(new { roles = ScreenCatalog.ConfigurableRoles, screens });
    }

    // POST, not PUT -- the production host blocks PUT/DELETE at the server level.
    [RequireScreen("access-control")]
    [HttpPost]
    public async Task<IActionResult> Save(SaveScreenAccessRequest request)
    {
        try
        {
            var allowed = request.Allowed.ToDictionary(
                role => role.Key,
                role => (IReadOnlyDictionary<string, bool>)role.Value);
            await _screenAccessService.SaveMatrixAsync(allowed, User.FindFirst("sub")?.Value);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
