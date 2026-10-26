using System.ComponentModel.DataAnnotations;
using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/device-tokens")]
public class DeviceTokensController : ControllerBase
{
    private readonly IDeviceTokenRepository _repository;

    public DeviceTokensController(IDeviceTokenRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Register(DeviceTokenRequest request)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await _repository.UpsertAsync(userId, request.Token, request.Platform, DateTime.UtcNow);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery, Required] string token)
    {
        await _repository.RemoveTokenAsync(token);
        return NoContent();
    }
}
