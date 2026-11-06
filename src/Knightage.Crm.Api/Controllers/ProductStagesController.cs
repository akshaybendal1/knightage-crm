using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Knightage.Crm.Api.Authorization;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/product-stages")]
public class ProductStagesController : ControllerBase
{
    private readonly IProductStageRepository _repository;

    public ProductStagesController(IProductStageRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive() => Ok(await _repository.GetActiveAsync());

    [RequirePermission("pipeline-stages:add", "leads:add", "leads:edit", "sales-entry:add")]
    [HttpPost]
    public async Task<IActionResult> Create(ProductStageRequest request)
    {
        var stage = new ProductStage
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            SortOrder = request.SortOrder,
            IsActive = true,
            IsWon = request.IsWon,
            IsLost = request.IsLost,
            IsDefault = request.IsDefault
        };
        await _repository.CreateAsync(stage);
        return Ok(stage);
    }

    [RequirePermission("pipeline-stages:edit")]
    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductStageRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = request.Name;
        existing.SortOrder = request.SortOrder;
        existing.IsWon = request.IsWon;
        existing.IsLost = request.IsLost;
        existing.IsDefault = request.IsDefault;

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }

    [RequirePermission("pipeline-stages:delete")]
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, ProductStageStatusRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _repository.SetActiveAsync(id, request.IsActive);
        return NoContent();
    }
}
