using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/product-categories")]
public class ProductCategoriesController : ControllerBase
{
    private readonly IProductCategoryRepository _repository;

    public ProductCategoriesController(IProductCategoryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive() => Ok(await _repository.GetActiveAsync());

    [HttpPost]
    public async Task<IActionResult> Create(ProductCategoryRequest request)
    {
        var category = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            SortOrder = request.SortOrder,
            IsActive = true
        };
        await _repository.CreateAsync(category);
        return Ok(category);
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductCategoryRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = request.Name;
        existing.SortOrder = request.SortOrder;

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, ProductCategoryStatusRequest request)
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
