using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    // Open to any authenticated user (not just Owner/Admin) -- Members need this to
    // populate the product dropdown when creating/editing their own leads, even though
    // they can't manage the catalog itself.
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request)
    {
        if (request.Price < 0)
        {
            return BadRequest(new { message = "price cannot be negative." });
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Price = request.Price,
            IsActive = true,
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _repository.CreateAsync(product);
        return Ok(product);
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductRequest request)
    {
        if (request.Price < 0)
        {
            return BadRequest(new { message = "price cannot be negative." });
        }

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Category = request.Category;
        existing.Price = request.Price;
        existing.UpdatedByUserId = User.FindFirst("sub")?.Value;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, ProductStatusRequest request)
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
