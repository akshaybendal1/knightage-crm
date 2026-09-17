using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/message-templates")]
public class MessageTemplatesController : ControllerBase
{
    private readonly IMessageTemplateRepository _repository;

    public MessageTemplatesController(IMessageTemplateRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repository.GetAllAsync());

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(MessageTemplateRequest request)
    {
        var template = new MessageTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Body = request.Body,
            IsActive = true,
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _repository.CreateAsync(template);
        return Ok(template);
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, MessageTemplateRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = request.Name;
        existing.Body = request.Body;
        existing.UpdatedByUserId = User.FindFirst("sub")?.Value;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return Ok(existing);
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, MessageTemplateStatusRequest request)
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
