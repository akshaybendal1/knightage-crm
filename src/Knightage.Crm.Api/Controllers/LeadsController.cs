using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadImportParser _importParser;

    public LeadsController(ILeadRepository leadRepository, ILeadImportParser importParser)
    {
        _leadRepository = leadRepository;
        _importParser = importParser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? pipelineStageId) =>
        Ok(await _leadRepository.GetAllAsync(pipelineStageId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var lead = await _leadRepository.GetByIdAsync(id);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPost]
    public async Task<IActionResult> Create(LeadRequest request)
    {
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            PipelineStageId = request.PipelineStageId,
            Notes = request.Notes,
            Source = "Manual",
            CreatedAtUtc = DateTime.UtcNow
        };
        await _leadRepository.CreateAsync(lead);
        return CreatedAtAction(nameof(GetById), new { id = lead.Id }, lead);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, LeadRequest request)
    {
        var existing = await _leadRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = request.Name;
        existing.Email = request.Email;
        existing.Phone = request.Phone;
        existing.Company = request.Company;
        existing.PipelineStageId = request.PipelineStageId;
        existing.Notes = request.Notes;

        await _leadRepository.UpdateAsync(existing);
        return Ok(existing);
    }

    [HttpPost("import")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import(IFormFile file, [FromForm] Guid pipelineStageId)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A file is required." });
        }

        string csvContent;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            csvContent = await reader.ReadToEndAsync();
        }

        var parseResult = _importParser.Parse(csvContent);

        var leads = parseResult.Leads.Select(line => new Lead
        {
            Id = Guid.NewGuid(),
            Name = line.Name,
            Email = line.Email,
            Phone = line.Phone,
            Company = line.Company,
            PipelineStageId = pipelineStageId,
            Source = "Import",
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();

        if (leads.Count > 0)
        {
            await _leadRepository.CreateManyAsync(leads);
        }

        return Ok(new
        {
            importedCount = leads.Count,
            errors = parseResult.Errors
        });
    }
}
