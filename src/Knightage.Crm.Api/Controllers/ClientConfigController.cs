using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

/// <summary>
/// Runtime config for the bundled Angular app -- anonymous (needed before login),
/// server-side (appsettings.json), not baked into the Angular build. Same pattern as
/// knightage-accounting's ClientConfigController.
/// </summary>
[ApiController]
[Route("api/client-config")]
public class ClientConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ClientConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var identityBaseUrl = _configuration["Client:IdentityBaseUrl"]
            ?? throw new InvalidOperationException("Client:IdentityBaseUrl is not configured.");

        return Ok(new { identityBaseUrl });
    }
}
