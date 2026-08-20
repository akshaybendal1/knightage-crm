using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/pipeline-stages")]
public class PipelineStagesController : ControllerBase
{
    private readonly IPipelineStageRepository _repository;

    public PipelineStagesController(IPipelineStageRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive() => Ok(await _repository.GetActiveAsync());

    [HttpPost]
    public async Task<IActionResult> Create(PipelineStageRequest request)
    {
        var stage = new PipelineStage
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            SortOrder = request.SortOrder,
            IsActive = true,
            IsWon = request.IsWon,
            IsLost = request.IsLost
        };
        await _repository.CreateAsync(stage);
        return Ok(stage);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PipelineStageRequest request)
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

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }
}
