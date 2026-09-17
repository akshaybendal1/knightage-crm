using System.Text.Json;
using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

// [Authorize(Roles = "Owner,Admin")] at class level, matching ReportsController itself
// (the parent feature) -- Reports as a whole is already fully admin-gated, unlike
// MessageTemplatesController's per-action layering.
[Authorize(Roles = "Owner,Admin")]
[ApiController]
[Route("api/custom-reports")]
public class CustomReportsController : ControllerBase
{
    private static readonly HashSet<string> ViewTypes = new() { "table", "bar", "line", "pie", "pivot" };

    private readonly ICustomReportRepository _customReportRepository;
    private readonly ISavedCustomReportRepository _savedReportRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IPipelineStageRepository _stageRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIdentityUserDirectory _identityUserDirectory;
    private readonly IReportExcelExporter _excelExporter;

    public CustomReportsController(
        ICustomReportRepository customReportRepository,
        ISavedCustomReportRepository savedReportRepository,
        ILeadRepository leadRepository,
        IPipelineStageRepository stageRepository,
        IProductRepository productRepository,
        IIdentityUserDirectory identityUserDirectory,
        IReportExcelExporter excelExporter)
    {
        _customReportRepository = customReportRepository;
        _savedReportRepository = savedReportRepository;
        _leadRepository = leadRepository;
        _stageRepository = stageRepository;
        _productRepository = productRepository;
        _identityUserDirectory = identityUserDirectory;
        _excelExporter = excelExporter;
    }

    // Live preview -- works for both an in-progress unsaved report and re-running a
    // saved one (the builder page always calls this on every control change).
    [HttpPost("run")]
    public async Task<IActionResult> Run(CustomReportDefinition request)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var items = await RunDefinitionAsync(request);
        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var reports = await _savedReportRepository.GetAllAsync();
        return Ok(reports.Select(r => new { r.Id, r.Name, r.UpdatedAtUtc }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var report = await _savedReportRepository.GetByIdAsync(id);
        if (report is null)
        {
            return NotFound();
        }

        var definition = JsonSerializer.Deserialize<CustomReportDefinition>(report.DefinitionJson);
        return Ok(new { report.Id, report.Name, definition });
    }

    // Convenience for opening a saved report: load + run in one round trip.
    [HttpGet("{id:guid}/run")]
    public async Task<IActionResult> RunSaved(Guid id)
    {
        var report = await _savedReportRepository.GetByIdAsync(id);
        if (report is null)
        {
            return NotFound();
        }

        var definition = JsonSerializer.Deserialize<CustomReportDefinition>(report.DefinitionJson);
        if (definition is null)
        {
            return StatusCode(500, new { message = "Saved report definition is corrupt." });
        }

        var items = await RunDefinitionAsync(definition);
        return Ok(new { report.Id, report.Name, definition, items });
    }

    [HttpPost]
    public async Task<IActionResult> Save(SaveCustomReportRequest request)
    {
        var validationError = Validate(request.Definition);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var report = new SavedCustomReport
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DefinitionJson = JsonSerializer.Serialize(request.Definition),
            IsActive = true,
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _savedReportRepository.CreateAsync(report);
        return CreatedAtAction(nameof(GetById), new { id = report.Id }, new { report.Id, report.Name });
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveCustomReportRequest request)
    {
        var existing = await _savedReportRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var validationError = Validate(request.Definition);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        existing.Name = request.Name;
        existing.DefinitionJson = JsonSerializer.Serialize(request.Definition);
        existing.UpdatedByUserId = User.FindFirst("sub")?.Value;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await _savedReportRepository.UpdateAsync(existing);
        return Ok(new { existing.Id, existing.Name });
    }

    // Soft delete/restore -- identical shape to this session's Leads/Stages delete endpoints.
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, CustomReportStatusRequest request)
    {
        var existing = await _savedReportRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _savedReportRepository.SetActiveAsync(id, request.IsActive);
        return NoContent();
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] List<string> dimensions,
        [FromQuery] string viewType = "table",
        [FromQuery] string range = "week",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] List<Guid>? stageIds = null,
        [FromQuery] List<string>? ownerUserIds = null,
        [FromQuery] List<Guid>? productIds = null,
        [FromQuery] List<string>? sources = null)
    {
        var definition = new CustomReportDefinition(dimensions, viewType, range, from, to, stageIds, ownerUserIds, productIds, sources);
        var validationError = Validate(definition);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var items = await RunDefinitionAsync(definition);
        var headers = definition.Dimensions.Count > 1
            ? new[] { "Dimension 1", "Dimension 2", "Lead Count", "Total Value" }
            : new[] { "Dimension", "Lead Count", "Total Value" };
        var rows = definition.Dimensions.Count > 1
            ? items.Select(i => new object[] { i.Dim1Label, i.Dim2Label ?? "", i.LeadCount, i.TotalValue }).ToList()
            : items.Select(i => new object[] { i.Dim1Label, i.LeadCount, i.TotalValue }).ToList();
        var content = _excelExporter.BuildCustomReport(headers, rows);
        var fileName = $"custom-report-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // Doubles as request validation (mirrors ReportsController's ReportFileNames
    // dictionary-as-validation trick) -- bad input is a clean 400, never reaches the
    // query builder.
    private static string? Validate(CustomReportDefinition definition)
    {
        if (definition.Dimensions is null || definition.Dimensions.Count is < 1 or > 2)
        {
            return "Pick 1 or 2 dimensions.";
        }
        if (definition.Dimensions.Distinct().Count() != definition.Dimensions.Count)
        {
            return "Dimensions must be distinct.";
        }
        if (definition.Dimensions.Any(d => !CustomReportDimensions.All.Contains(d)))
        {
            return "Unknown dimension.";
        }
        if (!ViewTypes.Contains(definition.ViewType))
        {
            return "Unknown view type.";
        }
        if (definition.ViewType == "line" && !definition.Dimensions.Contains(CustomReportDimensions.CreatedDate))
        {
            return "A line chart needs 'Created date' as one of the dimensions.";
        }
        if (definition.ViewType == "pivot" && definition.Dimensions.Count != 2)
        {
            return "A pivot needs exactly 2 dimensions.";
        }
        if ((definition.ViewType == "bar" || definition.ViewType == "pie") && definition.Dimensions.Count != 1)
        {
            return "A bar or pie chart needs exactly 1 dimension.";
        }
        return null;
    }

    private async Task<List<CustomReportRow>> RunDefinitionAsync(CustomReportDefinition definition)
    {
        var (since, until, bucketByMonth) = ReportsController.ResolveRange(definition.Range, definition.From, definition.To);
        var query = new CustomReportQuery(definition.Dimensions, since, until, bucketByMonth, definition.StageIds, definition.OwnerUserIds, definition.ProductIds, definition.Sources);
        var rows = await _customReportRepository.RunAsync(query);

        // OrdinalIgnoreCase on every dictionary here: Dim1Value/Dim2Value come back from
        // SQL Server's CAST(uniqueidentifier AS NVARCHAR) as UPPERCASE guid strings, while
        // C#'s Guid.ToString() produces lowercase -- a case-sensitive lookup would miss
        // every stage/product match and silently fall back to "Unknown".
        var stageNames = definition.Dimensions.Contains(CustomReportDimensions.Stage)
            ? (await _stageRepository.GetActiveAsync()).ToDictionary(s => s.Id.ToString(), s => s.Name, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var productNames = definition.Dimensions.Contains(CustomReportDimensions.Product)
            ? (await _productRepository.GetAllAsync()).ToDictionary(p => p.Id.ToString(), p => p.Name, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ownerNames = definition.Dimensions.Contains(CustomReportDimensions.Owner)
            ? (await _identityUserDirectory.GetOrganizationUsersAsync(Request.Headers.Authorization.ToString()))
                .ToDictionary(u => u.Id, u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Only the leads actually present in the grouped result need a name -- that's
        // bounded by the report's own filters, unlike a blind "pull every lead in the org"
        // query would be (this used to call GetPagedAsync with pageSize: 100_000, which on
        // an org with a few thousand leads pulled every column plus a LeadProducts cost
        // join, on every builder keystroke, just to read Id+Name off each row).
        var leadNames = definition.Dimensions.Contains(CustomReportDimensions.Lead)
            ? (await _leadRepository.GetNamesByIdsAsync(CollectDimensionGuids(rows, definition.Dimensions, CustomReportDimensions.Lead)))
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            row.Dim1Label = ResolveLabel(definition.Dimensions[0], row.Dim1Value, leadNames, stageNames, productNames, ownerNames);
            if (definition.Dimensions.Count > 1 && row.Dim2Value is not null)
            {
                row.Dim2Label = ResolveLabel(definition.Dimensions[1], row.Dim2Value, leadNames, stageNames, productNames, ownerNames);
            }
        }

        return rows.ToList();
    }

    private static IReadOnlyList<Guid> CollectDimensionGuids(IReadOnlyList<CustomReportRow> rows, List<string> dimensions, string dimension)
    {
        var index = dimensions.IndexOf(dimension);
        if (index < 0)
        {
            return Array.Empty<Guid>();
        }

        return rows
            .Select(r => index == 0 ? r.Dim1Value : r.Dim2Value)
            .Where(v => v is not null && Guid.TryParse(v, out _))
            .Select(v => Guid.Parse(v!))
            .Distinct()
            .ToList();
    }

    private static string ResolveLabel(
        string dimension,
        string value,
        Dictionary<string, string> leadNames,
        Dictionary<string, string> stageNames,
        Dictionary<string, string> productNames,
        Dictionary<string, string> ownerNames) => dimension switch
        {
            CustomReportDimensions.Lead => leadNames.GetValueOrDefault(value, "Unknown"),
            CustomReportDimensions.Stage => stageNames.GetValueOrDefault(value, "Unknown"),
            CustomReportDimensions.Product => value == Guid.Empty.ToString() ? "No product" : productNames.GetValueOrDefault(value, "Unknown"),
            CustomReportDimensions.Category => string.IsNullOrEmpty(value) ? "Uncategorized" : value,
            CustomReportDimensions.Owner => string.IsNullOrEmpty(value) ? "Unassigned" : ownerNames.GetValueOrDefault(value, $"User {value[..Math.Min(8, value.Length)]}"),
            _ => value, // source, createdDate -- already display-ready
        };
}
