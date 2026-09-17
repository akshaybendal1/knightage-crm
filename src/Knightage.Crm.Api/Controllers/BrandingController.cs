using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/branding")]
public class BrandingController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
    };
    private const long MaxLogoBytes = 2 * 1024 * 1024;

    private readonly IOrganizationBrandingRepository _repository;

    public BrandingController(IOrganizationBrandingRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo()
    {
        var branding = await _repository.GetAsync();
        if (branding is null)
        {
            return NotFound();
        }

        return File(branding.LogoData, branding.LogoContentType);
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Choose an image file to upload." });
        }
        if (file.Length > MaxLogoBytes)
        {
            return BadRequest(new { message = "Logo must be 2 MB or smaller." });
        }
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Logo must be a PNG or JPEG image." });
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        await _repository.UpsertAsync(file.ContentType, stream.ToArray(), User.FindFirst("sub")?.Value);
        return Ok();
    }

    // POST, not HTTP DELETE -- this app's production host rejects the DELETE
    // verb outright (confirmed via a live 405 from IIS/Plesk, not the app
    // itself), which is why every other soft-delete/removal action in this
    // codebase (Leads, Pipeline/Product Stages, Message Templates, Custom
    // Reports) is already a POST ".../status"-style endpoint rather than a
    // real DELETE. Mirrors that convention instead of introducing the first
    // DELETE verb in the app.
    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("logo/remove")]
    public async Task<IActionResult> RemoveLogo()
    {
        await _repository.DeleteAsync();
        return NoContent();
    }
}
