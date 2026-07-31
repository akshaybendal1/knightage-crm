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
            IsActive = true
        };
        await _repository.CreateAsync(stage);
        return Ok(stage);
    }
}
