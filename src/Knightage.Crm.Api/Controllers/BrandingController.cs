using System.Security.Claims;
using System.Security.Cryptography;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

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
    // Stored: big enough for the 40 pt-high logo on quote / invoice PDFs at print resolution, wide logos included.
    private const int StoredLogoMaxWidth = 1024, StoredLogoMaxHeight = 512;
    // Sent to the app: shown at most 48 px high and 200 px wide (Organization page; the sidebar is 36 x 160), so this
    // stays sharp on 3x phone screens. A square logo comes out 144 x 144.
    private const int ShownLogoMaxWidth = 600, ShownLogoMaxHeight = 144;

    private readonly IOrganizationBrandingRepository _repository;
    private readonly ILogoImageProcessor _images;
    private readonly IMemoryCache _cache;
    private readonly TenantContext _tenantContext;

    public BrandingController(IOrganizationBrandingRepository repository, ILogoImageProcessor images, IMemoryCache cache, TenantContext tenantContext)
    {
        _repository = repository;
        _images = images;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    private sealed record ShownLogo(LogoImage Image, string ETag);

    // Every page load asks for this. It is sent at display size (made once per uploaded logo and kept in memory; the stored
    // logo itself is not changed, so logos uploaded before the size limit are fixed too) with an ETag, so a browser that
    // already has it gets a bodyless 304. "no-cache" makes it ask each time, so a newly uploaded logo shows at once.
    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo()
    {
        var branding = await _repository.GetAsync();
        if (branding is null)
        {
            return NotFound();
        }

        var key = $"branding-logo-shown:{_tenantContext.RequireOrganizationId()}:{branding.UpdatedAtUtc.Ticks}:{branding.LogoData.Length}";
        var shown = _cache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var image = _images.Fit(branding.LogoData, branding.LogoContentType, ShownLogoMaxWidth, ShownLogoMaxHeight)
                ?? new LogoImage(branding.LogoData, branding.LogoContentType);
            return new ShownLogo(image, $"\"{Convert.ToHexString(SHA256.HashData(image.Data))[..20]}\"");
        })!;

        Response.Headers.CacheControl = "private, no-cache";
        return File(shown.Image.Data, shown.Image.ContentType, lastModified: null, entityTag: new EntityTagHeaderValue(shown.ETag));
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
        var logo = _images.Fit(stream.ToArray(), file.ContentType, StoredLogoMaxWidth, StoredLogoMaxHeight);
        if (logo is null)
        {
            return BadRequest(new { message = "That file isn't a PNG or JPEG image we can read. Try saving it again as PNG." });
        }

        // The JWT handler renames "sub" to ClaimTypes.NameIdentifier on the way in; accept either form.
        var userId = (User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"))?.Value;
        await _repository.UpsertAsync(logo.ContentType, logo.Data, userId);
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
