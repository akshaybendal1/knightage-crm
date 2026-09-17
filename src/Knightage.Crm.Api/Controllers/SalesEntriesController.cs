using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Knightage.Crm.Core.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

// Field Force Tracking: a "sales entry" is a regular Lead (Source = "FieldSales"),
// owned by the Sales rep who logged it, dropped straight into whichever Pipeline
// Stage the admin has marked Default (falling back to Won) -- see CreateAsync.
// Gated end-to-end behind knightage-platform's per-tenant FieldForceTrackingEnabled
// flag (checked here as defense in depth; the UI/nav already hides this from a
// disabled org).
[Authorize]
[ApiController]
[Route("api/sales-entries")]
public class SalesEntriesController : ControllerBase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadActivityRepository _activityRepository;
    private readonly IPipelineStageRepository _stageRepository;
    private readonly IProductStageRepository _productStageRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILeadProductRepository _leadProductRepository;
    private readonly IIdentityUserDirectory _identityUserDirectory;
    private readonly ISubscriptionStatusProvider _subscriptionStatusProvider;
    private readonly TenantContext _tenantContext;

    public SalesEntriesController(
        ILeadRepository leadRepository,
        ILeadActivityRepository activityRepository,
        IPipelineStageRepository stageRepository,
        IProductStageRepository productStageRepository,
        IProductRepository productRepository,
        ILeadProductRepository leadProductRepository,
        IIdentityUserDirectory identityUserDirectory,
        ISubscriptionStatusProvider subscriptionStatusProvider,
        TenantContext tenantContext)
    {
        _leadRepository = leadRepository;
        _activityRepository = activityRepository;
        _stageRepository = stageRepository;
        _productStageRepository = productStageRepository;
        _productRepository = productRepository;
        _leadProductRepository = leadProductRepository;
        _identityUserDirectory = identityUserDirectory;
        _subscriptionStatusProvider = subscriptionStatusProvider;
        _tenantContext = tenantContext;
    }

    private async Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken)
    {
        var organizationId = _tenantContext.RequireOrganizationId();
        var authorizationHeader = Request.Headers.Authorization.ToString();
        // Already computed once per request by TenantResolutionMiddleware -- a cache
        // hit (see PlatformSubscriptionStatusProvider), not a second Platform round-trip.
        var details = await _subscriptionStatusProvider.GetDetailsAsync(organizationId, authorizationHeader, cancellationToken);
        return details.FieldForceTrackingEnabled;
    }

    [HttpPost]
    public async Task<IActionResult> Create(SalesEntryRequest request, CancellationToken cancellationToken)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Field Force Tracking isn't enabled for this organization." });
        }

        var stages = await _stageRepository.GetActiveAsync();
        // IsDefault wins when an admin has set it; IsWon is the fallback so an org that
        // configured this before IsDefault existed keeps working unchanged.
        var targetStage = stages.Where(s => s.IsDefault).OrderBy(s => s.SortOrder).FirstOrDefault()
            ?? stages.Where(s => s.IsWon).OrderBy(s => s.SortOrder).FirstOrDefault();
        if (targetStage is null)
        {
            return BadRequest(new { message = "No pipeline stage is marked as Default (or Won) yet. Ask your admin to mark one on the Pipeline Stages page before logging sales entries." });
        }

        var productStages = await _productStageRepository.GetActiveAsync();
        var productStagesById = productStages.ToDictionary(s => s.Id);
        var defaultProductStage = productStages.Where(s => s.IsDefault).OrderBy(s => s.SortOrder).FirstOrDefault()
            ?? productStages.Where(s => s.IsWon).OrderBy(s => s.SortOrder).FirstOrDefault();

        foreach (var line in request.Products)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId);
            if (product is null)
            {
                return BadRequest(new { message = $"productId {line.ProductId} does not refer to an existing product." });
            }

            if (line.ProductStageId is Guid requestedStageId && !productStagesById.ContainsKey(requestedStageId))
            {
                return BadRequest(new { message = $"productStageId {requestedStageId} does not refer to an existing product stage." });
            }

            if (line.ProductStageId is null && defaultProductStage is null)
            {
                return BadRequest(new { message = "No product stage is marked as Default (or Won) yet. Ask your admin to mark one on the Product Stages page, or pick a stage for each product." });
            }
        }

        var currentUserId = User.FindFirst("sub")?.Value;
        var now = DateTime.UtcNow;
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            Name = request.CustomerName,
            Company = request.Company,
            Email = request.Email,
            Phone = request.Phone,
            PipelineStageId = targetStage.Id,
            Notes = request.Notes,
            Source = "FieldSales",
            // Always the current user -- unlike LeadsController.Create, this is never
            // caller-overridable: the whole point of a sales entry is rep-owns-it.
            OwnerUserId = currentUserId,
            CreatedByUserId = currentUserId,
            CreatedAtUtc = now,
            PipelineStageChangedAtUtc = now,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationCapturedAtUtc = now,
        };
        await _leadRepository.CreateAsync(lead);

        foreach (var line in request.Products)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId);
            var addedAtUtc = DateTime.UtcNow;
            await _leadProductRepository.AddAsync(new LeadProduct
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = product!.Price,
                CreatedAtUtc = addedAtUtc,
                ProductStageId = line.ProductStageId ?? defaultProductStage!.Id,
                StatusChangedAtUtc = addedAtUtc,
            });
        }

        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = lead.Id,
            Type = "LeadCreated",
            Content = "Sales entry logged from the field",
            CreatedByUserId = currentUserId,
            CreatedAtUtc = now,
        });

        return StatusCode(StatusCodes.Status201Created, lead);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Field Force Tracking isn't enabled for this organization." });
        }

        var currentUserId = User.FindFirst("sub")?.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, totalCount) = await _leadRepository.GetFieldSalesAsync(currentUserId, page, pageSize);
        return Ok(new { items, totalCount });
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ownerUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Field Force Tracking isn't enabled for this organization." });
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, totalCount) = await _leadRepository.GetFieldSalesAsync(ownerUserId, page, pageSize);

        var authorizationHeader = Request.Headers.Authorization.ToString();
        var users = (await _identityUserDirectory.GetOrganizationUsersAsync(authorizationHeader, cancellationToken))
            .ToDictionary(u => u.Id, u => u.DisplayName, StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            item.OwnerDisplayName = item.OwnerUserId is not null && users.TryGetValue(item.OwnerUserId, out var displayName)
                ? displayName
                : "Unknown";
        }

        return Ok(new { items, totalCount });
    }

    // Backs the Sales Dashboard's admin-only coverage map/area view: every one of a
    // single rep's sales-entry locations within a date range, unpaged. The caller
    // already knows the rep's display name (from the org's user list it fetched for
    // the rep dropdown), so there's no identity lookup here.
    [Authorize(Roles = "Owner,Admin")]
    [HttpGet("coverage")]
    public async Task<IActionResult> GetCoverage([FromQuery] string ownerUserId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Field Force Tracking isn't enabled for this organization." });
        }

        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return BadRequest(new { message = "ownerUserId is required." });
        }

        var fromUtc = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
        if (toUtc <= fromUtc || (toUtc - fromUtc).TotalDays > 92)
        {
            return BadRequest(new { message = "Date range must be valid and no more than 92 days." });
        }

        var items = await _leadRepository.GetFieldSalesLocationsAsync(ownerUserId, fromUtc, toUtc);
        return Ok(new { items });
    }

    // Backs the Sales Dashboard's "Sales by rep" chart -- org-wide per-rep totals,
    // reusing ReportsController's range-resolution convention (week/month/all/custom)
    // since it's internal static and explicitly documented as shared across controllers.
    [Authorize(Roles = "Owner,Admin")]
    [HttpGet("by-rep")]
    public async Task<IActionResult> GetByRep([FromQuery] string range = "week", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, CancellationToken cancellationToken = default)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Field Force Tracking isn't enabled for this organization." });
        }

        var (since, until, _) = ReportsController.ResolveRange(range, from, to);
        var raw = await _leadRepository.GetFieldSalesByRepRawAsync(since, until);

        var authorizationHeader = Request.Headers.Authorization.ToString();
        var users = (await _identityUserDirectory.GetOrganizationUsersAsync(authorizationHeader, cancellationToken))
            .ToDictionary(u => u.Id, u => u.DisplayName, StringComparer.OrdinalIgnoreCase);

        var items = raw.Select(r => new
        {
            ownerUserId = r.OwnerUserId,
            ownerName = !string.IsNullOrEmpty(r.OwnerUserId) && users.TryGetValue(r.OwnerUserId, out var displayName) ? displayName : "Unassigned",
            entryCount = r.EntryCount,
            totalValue = r.TotalValue,
        });

        return Ok(new { items });
    }
}
