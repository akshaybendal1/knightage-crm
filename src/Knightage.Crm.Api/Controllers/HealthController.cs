using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", service = "knightage-crm" });
}
