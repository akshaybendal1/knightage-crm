using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/lead-products")]
public class LeadProductsController : ControllerBase
{
    private readonly ILeadProductRepository _leadProductRepository;
    private readonly ILeadActivityRepository _activityRepository;
    private readonly IProductStageRepository _productStageRepository;

    public LeadProductsController(ILeadProductRepository leadProductRepository, ILeadActivityRepository activityRepository, IProductStageRepository productStageRepository)
    {
        _leadProductRepository = leadProductRepository;
        _activityRepository = activityRepository;
        _productStageRepository = productStageRepository;
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

        if (existing.ProductStageId != request.ProductStageId)
        {
            var now = DateTime.UtcNow;
            await _leadProductRepository.UpdateProductStageAsync(id, request.ProductStageId, now);
            await _activityRepository.CreateAsync(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = existing.LeadId,
                Type = "ProductStatusChange",
                Content = $"Product moved to {stage.Name}",
                CreatedByUserId = User.FindFirst("sub")?.Value,
                CreatedAtUtc = now
            });
            existing.ProductStageId = request.ProductStageId;
            existing.ProductStageName = stage.Name;
            existing.StatusChangedAtUtc = now;
        }

        return Ok(existing);
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
