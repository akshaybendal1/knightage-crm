using System.Text.Json;
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
    private readonly ILeadActivityRepository _activityRepository;
    private readonly IPipelineStageRepository _stageRepository;
    private readonly ILeadTaskRepository _taskRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IIdentityCalendarClient _identityCalendarClient;
    private readonly ILeadProductRepository _leadProductRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IProductStageRepository _productStageRepository;
    private readonly IIdentityUserDirectory _identityUserDirectory;
    private readonly IReportExcelExporter _excelExporter;

    public LeadsController(
        ILeadRepository leadRepository,
        ILeadImportParser importParser,
        ILeadActivityRepository activityRepository,
        IPipelineStageRepository stageRepository,
        ILeadTaskRepository taskRepository,
        IProductRepository productRepository,
        IMeetingRepository meetingRepository,
        IIdentityCalendarClient identityCalendarClient,
        ILeadProductRepository leadProductRepository,
        IDocumentRepository documentRepository,
        IProductStageRepository productStageRepository,
        IIdentityUserDirectory identityUserDirectory,
        IReportExcelExporter excelExporter)
    {
        _leadRepository = leadRepository;
        _importParser = importParser;
        _activityRepository = activityRepository;
        _stageRepository = stageRepository;
        _taskRepository = taskRepository;
        _productRepository = productRepository;
        _meetingRepository = meetingRepository;
        _identityCalendarClient = identityCalendarClient;
        _leadProductRepository = leadProductRepository;
        _documentRepository = documentRepository;
        _productStageRepository = productStageRepository;
        _identityUserDirectory = identityUserDirectory;
        _excelExporter = excelExporter;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? pipelineStageId, [FromQuery] Guid? productId, [FromQuery] string? ownerUserId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, totalCount) = await _leadRepository.GetPagedAsync(pipelineStageId, productId, ownerUserId, search, page, pageSize);
        return Ok(new { items, totalCount });
    }

    // Respects the exact same filters as GetAll but returns every matching row (no
    // paging) as a single workbook -- reuses GetPagedAsync with a page big enough to
    // cover this org's realistic lead volume rather than adding a parallel unpaged
    // query, mirroring ReportsController's own "export what you'd otherwise page
    // through" convention.
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] Guid? pipelineStageId, [FromQuery] Guid? productId, [FromQuery] string? ownerUserId, [FromQuery] string? search)
    {
        var (items, _) = await _leadRepository.GetPagedAsync(pipelineStageId, productId, ownerUserId, search, page: 1, pageSize: 100_000);

        var stages = await _stageRepository.GetActiveAsync();
        var stageNames = stages.ToDictionary(s => s.Id, s => s.Name);

        var authorizationHeader = Request.Headers.Authorization.ToString();
        var users = (await _identityUserDirectory.GetOrganizationUsersAsync(authorizationHeader))
            .ToDictionary(u => u.Id, u => u.DisplayName, StringComparer.OrdinalIgnoreCase);

        var rows = items.Select(l => new LeadExportRow
        {
            Name = l.Name,
            Email = l.Email,
            Phone = l.Phone,
            Company = l.Company,
            StageName = stageNames.GetValueOrDefault(l.PipelineStageId, "Unknown"),
            OwnerName = l.OwnerUserId is not null && users.TryGetValue(l.OwnerUserId, out var displayName) ? displayName : "Unassigned",
            Source = l.Source,
            TotalCost = l.TotalCost,
        }).ToList();

        var content = _excelExporter.BuildLeadsExport(rows);
        var fileName = $"leads-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var lead = await _leadRepository.GetByIdAsync(id);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPost]
    public async Task<IActionResult> Create(LeadRequest request)
    {
        // [Required] on PipelineStageId is a no-op -- Guid is a non-nullable value type, so a
        // missing field in the request body just model-binds to Guid.Empty, which satisfies
        // [Required] fine. Without this check, both a missing id and a genuinely nonexistent
        // one fall through to the INSERT and blow up as an unhandled FK-constraint exception
        // (500) instead of a clean validation error.
        var stage = await _stageRepository.GetByIdAsync(request.PipelineStageId);
        if (stage is null)
        {
            return BadRequest(new { message = "pipelineStageId does not refer to an existing pipeline stage." });
        }

        var currentUserId = User.FindFirst("sub")?.Value;
        var now = DateTime.UtcNow;
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
            // Every lead gets an owner: defaults to whoever created it, same as
            // LeadTask.AssignedToUserId already defaults to the creator.
            OwnerUserId = string.IsNullOrWhiteSpace(request.OwnerUserId) ? currentUserId : request.OwnerUserId,
            CreatedByUserId = currentUserId,
            CreatedAtUtc = now,
            PipelineStageChangedAtUtc = now
        };
        await _leadRepository.CreateAsync(lead);
        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = lead.Id,
            Type = "LeadCreated",
            Content = "Lead created manually",
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = lead.CreatedAtUtc
        });
        return CreatedAtAction(nameof(GetById), new { id = lead.Id }, lead);
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, LeadRequest request)
    {
        var existing = await _leadRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var stage = await _stageRepository.GetByIdAsync(request.PipelineStageId);
        if (stage is null)
        {
            return BadRequest(new { message = "pipelineStageId does not refer to an existing pipeline stage." });
        }

        var previousStageId = existing.PipelineStageId;

        existing.Name = request.Name;
        existing.Email = request.Email;
        existing.Phone = request.Phone;
        existing.Company = request.Company;
        existing.PipelineStageId = request.PipelineStageId;
        if (previousStageId != request.PipelineStageId)
        {
            existing.PipelineStageChangedAtUtc = DateTime.UtcNow;
        }
        existing.Notes = request.Notes;
        // Unlike Create, a blank OwnerUserId here does NOT default to the current editor --
        // that would silently reassign ownership on every unrelated field edit. Keep the
        // existing owner unless the caller explicitly sent a new one.
        existing.OwnerUserId = string.IsNullOrWhiteSpace(request.OwnerUserId) ? existing.OwnerUserId : request.OwnerUserId;
        existing.UpdatedByUserId = User.FindFirst("sub")?.Value;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _leadRepository.UpdateAsync(existing);

        if (previousStageId != request.PipelineStageId)
        {
            var fromStage = await _stageRepository.GetByIdAsync(previousStageId);
            var metadata = JsonSerializer.Serialize(new
            {
                fromStageId = previousStageId,
                fromStage = fromStage?.Name ?? "Unknown",
                toStageId = request.PipelineStageId,
                toStage = stage.Name
            });

            await _activityRepository.CreateAsync(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = id,
                Type = "StageChange",
                Content = $"Moved from {fromStage?.Name ?? "Unknown"} to {stage.Name}",
                Metadata = metadata,
                CreatedByUserId = User.FindFirst("sub")?.Value,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return Ok(existing);
    }

    // Lightweight stage-only move, for the Timeline board's drag-and-drop (and Android's
    // long-press move dialog) -- Update above requires the entire LeadRequest, which is
    // unusable for a quick card move. Reuses the exact same StageChange activity shape
    // Update already logs.
    [HttpPost("{id:guid}/stage")]
    public async Task<IActionResult> UpdateStage(Guid id, UpdateLeadStageRequest request)
    {
        var existing = await _leadRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var stage = await _stageRepository.GetByIdAsync(request.PipelineStageId);
        if (stage is null)
        {
            return BadRequest(new { message = "pipelineStageId does not refer to an existing pipeline stage." });
        }

        if (existing.PipelineStageId != request.PipelineStageId)
        {
            var previousStageId = existing.PipelineStageId;
            var fromStage = await _stageRepository.GetByIdAsync(previousStageId);
            var now = DateTime.UtcNow;

            await _leadRepository.UpdateStageAsync(id, request.PipelineStageId, now);

            var metadata = JsonSerializer.Serialize(new
            {
                fromStageId = previousStageId,
                fromStage = fromStage?.Name ?? "Unknown",
                toStageId = request.PipelineStageId,
                toStage = stage.Name
            });

            await _activityRepository.CreateAsync(new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = id,
                Type = "StageChange",
                Content = $"Moved from {fromStage?.Name ?? "Unknown"} to {stage.Name}",
                Metadata = metadata,
                CreatedByUserId = User.FindFirst("sub")?.Value,
                CreatedAtUtc = now
            });

            existing.PipelineStageId = request.PipelineStageId;
            existing.PipelineStageChangedAtUtc = now;
        }

        return Ok(existing);
    }

    // Soft delete: sets IsActive = false so GetPagedAsync (and by extension the Leads
    // list, Timeline, Dashboard, and Reports queries) stop returning this lead, without
    // touching its Tasks/Meetings/Documents history. GetByIdAsync intentionally does NOT
    // filter on IsActive, so those child records (and this endpoint itself, for a
    // reactivate) keep resolving the lead fine.
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, LeadStatusRequest request)
    {
        var existing = await _leadRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _leadRepository.SetActiveAsync(id, request.IsActive);
        return NoContent();
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
        var createdByUserId = User.FindFirst("sub")?.Value;
        var importedAtUtc = DateTime.UtcNow;

        var leads = parseResult.Leads.Select(line => new Lead
        {
            Id = Guid.NewGuid(),
            Name = line.Name,
            Email = line.Email,
            Phone = line.Phone,
            Company = line.Company,
            PipelineStageId = pipelineStageId,
            Source = "Import",
            OwnerUserId = createdByUserId,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = importedAtUtc,
            PipelineStageChangedAtUtc = importedAtUtc
        }).ToList();

        if (leads.Count > 0)
        {
            await _leadRepository.CreateManyAsync(leads);
            await _activityRepository.CreateManyAsync(leads.Select(lead => new LeadActivity
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Type = "LeadCreated",
                Content = "Lead created via CSV import",
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = lead.CreatedAtUtc
            }));
        }

        return Ok(new
        {
            importedCount = leads.Count,
            errors = parseResult.Errors
        });
    }

    [HttpGet("{leadId:guid}/activities")]
    public async Task<IActionResult> GetActivities(Guid leadId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, hasMore) = await _activityRepository.GetByLeadIdAsync(leadId, page, pageSize);
        return Ok(new { items, hasMore });
    }

    [HttpPost("{leadId:guid}/activities")]
    public async Task<IActionResult> CreateActivity(Guid leadId, LeadActivityRequest request)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        var activity = new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Note" : request.Type,
            Content = request.Content,
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _activityRepository.CreateAsync(activity);
        return CreatedAtAction(nameof(GetActivities), new { leadId }, activity);
    }

    [HttpGet("{leadId:guid}/tasks")]
    public async Task<IActionResult> GetTasks(Guid leadId)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        return Ok(await _taskRepository.GetByLeadIdAsync(leadId));
    }

    [HttpPost("{leadId:guid}/tasks")]
    public async Task<IActionResult> CreateTask(Guid leadId, CreateTaskRequest request)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirst("sub")?.Value;
        var task = new LeadTask
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate.Date,
            Status = "Open",
            AssignedToUserId = string.IsNullOrWhiteSpace(request.AssignedToUserId) ? currentUserId ?? string.Empty : request.AssignedToUserId,
            CreatedByUserId = currentUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _taskRepository.CreateAsync(task);
        return CreatedAtAction(nameof(GetTasks), new { leadId }, task);
    }

    [HttpGet("{leadId:guid}/meetings")]
    public async Task<IActionResult> GetMeetings(Guid leadId)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        return Ok(await _meetingRepository.GetByLeadIdAsync(leadId));
    }

    [HttpPost("{leadId:guid}/meetings")]
    public async Task<IActionResult> CreateMeeting(Guid leadId, CreateMeetingRequest request)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        if (request.EndAtUtc <= request.StartAtUtc)
        {
            return BadRequest(new { message = "endAtUtc must be after startAtUtc." });
        }

        var currentUserId = User.FindFirst("sub")?.Value;
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            Title = request.Title,
            Description = request.Description,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            Location = request.Location,
            Status = "Scheduled",
            OwnerUserId = string.IsNullOrWhiteSpace(request.OwnerUserId) ? currentUserId ?? string.Empty : request.OwnerUserId,
            CreatedByUserId = currentUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _meetingRepository.CreateAsync(meeting);

        // Best-effort: the meeting row above always saves. IIdentityCalendarClient itself
        // never throws (any failure already collapses to a Failed outcome inside it) -- this
        // try/catch is only a safety net so nothing about the cross-service call can turn a
        // successful meeting creation into a 500.
        try
        {
            var authorizationHeader = Request.Headers.Authorization.ToString();
            var result = await _identityCalendarClient.CreateEventAsync(
                meeting.OwnerUserId,
                new CalendarEventRequest(meeting.Title, meeting.Description, meeting.StartAtUtc, meeting.EndAtUtc, meeting.Location, lead.Email),
                authorizationHeader);
            var syncStatus = MapSyncStatus(result.Outcome);
            await _meetingRepository.UpdateGoogleSyncAsync(meeting.Id, result.GoogleEventId, syncStatus, result.Outcome == CalendarSyncOutcome.Failed ? DateTime.UtcNow : null);
            meeting.GoogleEventId = result.GoogleEventId;
            meeting.GoogleSyncStatus = syncStatus;
        }
        catch
        {
            await _meetingRepository.UpdateGoogleSyncAsync(meeting.Id, null, "Failed", DateTime.UtcNow);
            meeting.GoogleSyncStatus = "Failed";
        }

        return CreatedAtAction(nameof(GetMeetings), new { leadId }, meeting);
    }

    [HttpGet("{leadId:guid}/products")]
    public async Task<IActionResult> GetLeadProducts(Guid leadId)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        return Ok(await _leadProductRepository.GetByLeadIdAsync(leadId));
    }

    [HttpPost("{leadId:guid}/products")]
    public async Task<IActionResult> AddLeadProduct(Guid leadId, AddLeadProductRequest request)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        // Existence only, same as the old ProductId validation this replaces -- not
        // restricted to IsActive products server-side; that's a client-side picker
        // nicety, not a hard rule (an already-deactivated product can still be a valid
        // historical line item on a lead created before it was deactivated).
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product is null)
        {
            return BadRequest(new { message = "productId does not refer to an existing product." });
        }

        var stage = await _productStageRepository.GetByIdAsync(request.ProductStageId);
        if (stage is null)
        {
            return BadRequest(new { message = "productStageId does not refer to an existing product stage." });
        }

        var addedAtUtc = DateTime.UtcNow;
        var leadProduct = new LeadProduct
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitPrice = product.Price,
            CreatedAtUtc = addedAtUtc,
            ProductStageId = request.ProductStageId,
            StatusChangedAtUtc = addedAtUtc
        };
        await _leadProductRepository.AddAsync(leadProduct);
        leadProduct.ProductStageName = stage.Name;
        leadProduct.ProductName = product.Name;
        return CreatedAtAction(nameof(GetLeadProducts), new { leadId }, leadProduct);
    }

    [HttpGet("{leadId:guid}/documents")]
    public async Task<IActionResult> GetDocuments(Guid leadId)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        return Ok(await _documentRepository.GetByLeadIdAsync(leadId));
    }

    [HttpPost("{leadId:guid}/documents")]
    public async Task<IActionResult> CreateDocument(Guid leadId, CreateDocumentRequest request)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        if (request.Type is not ("Quotation" or "Invoice"))
        {
            return BadRequest(new { message = "Type must be 'Quotation' or 'Invoice'." });
        }

        // Snapshots ALL of the lead's current LeadProducts regardless of status --
        // matches Lead.TotalCost's no-status-filter semantics, so a freshly-created
        // document's Subtotal equals what's already shown as the lead's cost elsewhere.
        var leadProducts = await _leadProductRepository.GetByLeadIdAsync(leadId);
        if (leadProducts.Count == 0)
        {
            return BadRequest(new { message = "Add at least one product to this lead before generating a document." });
        }

        var lineItems = leadProducts.Select(lp => new DocumentLineItem
        {
            Id = Guid.NewGuid(),
            ProductName = lp.ProductName ?? "(unknown product)",
            Quantity = lp.Quantity,
            UnitPrice = lp.UnitPrice,
            LineTotal = lp.Quantity * lp.UnitPrice
        }).ToList();

        var subtotal = lineItems.Sum(li => li.LineTotal);
        var discountAmount = Math.Round(subtotal * request.DiscountPercent / 100m, 2);
        var taxableAmount = subtotal - discountAmount;
        var taxAmount = Math.Round(taxableAmount * request.TaxRatePercent / 100m, 2);
        var now = DateTime.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            Type = request.Type,
            Status = "Draft",
            DiscountPercent = request.DiscountPercent,
            DiscountAmount = discountAmount,
            TaxRatePercent = request.TaxRatePercent,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            Total = taxableAmount + taxAmount,
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = now
        };

        await _documentRepository.CreateAsync(document, lineItems);
        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            Type = "DocumentCreated",
            Content = $"{document.Type} {document.Number} created",
            CreatedByUserId = document.CreatedByUserId,
            CreatedAtUtc = now
        });

        return CreatedAtAction(nameof(GetDocuments), new { leadId }, document);
    }

    // NotConnected collapses into "NotSynced" -- from the CRM side both mean "nothing to
    // show but a connect prompt", so there's no separate DB state for them. ReauthRequired
    // stays distinct so the UI can prompt the owner to reconnect specifically, rather than
    // looking identical to "never tried".
    private static string MapSyncStatus(CalendarSyncOutcome outcome) => outcome switch
    {
        CalendarSyncOutcome.Synced => "Synced",
        CalendarSyncOutcome.ReauthRequired => "ReauthRequired",
        CalendarSyncOutcome.Failed => "Failed",
        _ => "NotSynced"
    };
}
