using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/lead-products")]
public class LeadProductsController : ControllerBase
{
    private readonly ILeadProductRepository _leadProductRepository;
    private readonly ILeadActivityRepository _activityRepository;
    private readonly IProductStageRepository _productStageRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<LeadProductsController> _logger;

    public LeadProductsController(
        ILeadProductRepository leadProductRepository,
        ILeadActivityRepository activityRepository,
        IProductStageRepository productStageRepository,
        IProductRepository productRepository,
        ILogger<LeadProductsController> logger)
    {
        _leadProductRepository = leadProductRepository;
        _activityRepository = activityRepository;
        _productStageRepository = productStageRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> UpdateQuantity(Guid id, UpdateLeadProductQuantityRequest request)
    {
        var existing = await _leadProductRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _leadProductRepository.UpdateQuantityAsync(id, request.Quantity);
        existing.Quantity = request.Quantity;
        return Ok(existing);
    }

    [HttpPost("{id:guid}/stage")]
    public async Task<IActionResult> UpdateStage(Guid id, UpdateLeadProductStageRequest request)
    {
        var existing = await _leadProductRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var stage = await _productStageRepository.GetByIdAsync(request.ProductStageId);
        if (stage is null)
        {
            return BadRequest(new { message = "productStageId does not refer to an existing product stage." });
        }

        await MoveToStageAsync(existing, stage, request.Note, new Dictionary<Guid, string>(), new Dictionary<Guid, string>());
        return Ok(existing);
    }

    // Moves many lead-product lines to one product stage in a single request, each with its own
    // optional note. Same contract as the leads bulk move: every line is moved and reported
    // on its own, so one missing or failing line never stops or undoes the others.
    [HttpPost("stage/bulk")]
    public async Task<IActionResult> BulkUpdateStage(BulkProductStageMoveRequest request)
    {
        if (request.Items.Count is 0 or > StageMoveLimits.MaxItems)
        {
            return BadRequest(new { message = $"Select between 1 and {StageMoveLimits.MaxItems} products to move at a time." });
        }

        var stage = await _productStageRepository.GetByIdAsync(request.ProductStageId);
        if (stage is null)
        {
            return BadRequest(new { message = "productStageId does not refer to an existing product stage." });
        }

        var stageNames = new Dictionary<Guid, string>();
        var productNames = new Dictionary<Guid, string>();
        var results = new List<StageMoveResult>(request.Items.Count);
        foreach (var item in request.Items.DistinctBy(i => i.Id))
        {
            try
            {
                var line = await _leadProductRepository.GetByIdAsync(item.Id);
                if (line is null)
                {
                    results.Add(new StageMoveResult(item.Id, "NotFound", "Product line not found."));
                    continue;
                }

                var moved = await MoveToStageAsync(line, stage, item.Note, stageNames, productNames);
                results.Add(moved
                    ? new StageMoveResult(item.Id, "Moved", null)
                    : new StageMoveResult(item.Id, "Unchanged", $"Already in {stage.Name}."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk stage move failed for lead product {LeadProductId}.", item.Id);
                results.Add(new StageMoveResult(item.Id, "Failed", "Could not move this product."));
            }
        }

        return Ok(new BulkStageMoveResponse(
            results.Count(r => r.Status == "Moved"),
            results.Count(r => r.Status == "Unchanged"),
            results.Count(r => r.Status is "NotFound" or "Failed"),
            results));
    }

    // Shared by the single and bulk moves. Returns false (and writes nothing) when the line is
    // already in the target stage. The activity text is unchanged from before; its metadata is
    // new and carries the product, the from/to stage and the optional note.
    private async Task<bool> MoveToStageAsync(
        LeadProduct existing, ProductStage stage, string? note,
        Dictionary<Guid, string> stageNames, Dictionary<Guid, string> productNames)
    {
        if (existing.ProductStageId == stage.Id)
        {
            return false;
        }

        var previousStageId = existing.ProductStageId;
        if (!stageNames.TryGetValue(previousStageId, out var fromStageName))
        {
            fromStageName = (await _productStageRepository.GetByIdAsync(previousStageId))?.Name ?? "Unknown";
            stageNames[previousStageId] = fromStageName;
        }
        if (!productNames.TryGetValue(existing.ProductId, out var productName))
        {
            productName = (await _productRepository.GetByIdAsync(existing.ProductId))?.Name ?? "Unknown product";
            productNames[existing.ProductId] = productName;
        }

        var now = DateTime.UtcNow;
        await _leadProductRepository.UpdateProductStageAsync(existing.Id, stage.Id, now);

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var metadata = new Dictionary<string, object>
        {
            ["leadProductId"] = existing.Id,
            ["productName"] = productName,
            ["fromStageId"] = previousStageId,
            ["fromStage"] = fromStageName,
            ["toStageId"] = stage.Id,
            ["toStage"] = stage.Name,
        };
        if (trimmedNote is not null)
        {
            metadata["note"] = trimmedNote;
        }

        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = existing.LeadId,
            Type = "ProductStatusChange",
            Content = $"Product moved to {stage.Name}",
            Metadata = JsonSerializer.Serialize(metadata),
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = now
        });

        existing.ProductStageId = stage.Id;
        existing.ProductStageName = stage.Name;
        existing.StatusChangedAtUtc = now;
        return true;
    }

    [HttpPost("{id:guid}/remove")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var existing = await _leadProductRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _leadProductRepository.RemoveAsync(id);
        return NoContent();
    }
}
