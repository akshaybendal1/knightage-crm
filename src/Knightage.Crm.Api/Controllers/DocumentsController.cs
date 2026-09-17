using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

// Flat item-level ops on an existing document, mirroring LeadProductsController's
// split from LeadsController (list/create nested under the lead; everything else here).
[Authorize]
[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IIdentityOrganizationClient _organizationClient;
    private readonly IDocumentPdfBuilder _pdfBuilder;
    private readonly IOrganizationBrandingRepository _brandingRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILeadActivityRepository _activityRepository;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentRepository documentRepository,
        ILeadRepository leadRepository,
        IIdentityOrganizationClient organizationClient,
        IDocumentPdfBuilder pdfBuilder,
        IOrganizationBrandingRepository brandingRepository,
        IEmailSender emailSender,
        ILeadActivityRepository activityRepository,
        ILogger<DocumentsController> logger)
    {
        _documentRepository = documentRepository;
        _leadRepository = leadRepository;
        _organizationClient = organizationClient;
        _pdfBuilder = pdfBuilder;
        _brandingRepository = brandingRepository;
        _emailSender = emailSender;
        _activityRepository = activityRepository;
        _logger = logger;
    }

    // No status restriction -- Drafts are downloadable/previewable too.
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        var lead = await _leadRepository.GetByIdAsync(document.LeadId);
        if (lead is null)
        {
            return NotFound();
        }

        var lineItems = await _documentRepository.GetLineItemsAsync(id);
        var organization = await GetOrganizationOrFallbackAsync(cancellationToken);
        var branding = await _brandingRepository.GetAsync();
        var pdfBytes = _pdfBuilder.Build(document, lineItems, lead, organization, branding?.LogoData);
        return File(pdfBytes, "application/pdf", $"{document.Number}.pdf");
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        var lead = await _leadRepository.GetByIdAsync(document.LeadId);
        if (lead is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(lead.Email))
        {
            return BadRequest(new { message = "This lead has no email on file -- add one before sending." });
        }

        var lineItems = await _documentRepository.GetLineItemsAsync(id);
        var organization = await GetOrganizationOrFallbackAsync(cancellationToken);
        var branding = await _brandingRepository.GetAsync();
        var pdfBytes = _pdfBuilder.Build(document, lineItems, lead, organization, branding?.LogoData);

        var subject = $"{document.Type} {document.Number} from {organization.Name}";
        var body = $"""
            Hi {lead.Name},

            Please find attached {document.Type.ToLowerInvariant()} {document.Number} for {document.Total:C2}.

            Thanks,
            {organization.Name}
            """;

        try
        {
            await _emailSender.SendWithAttachmentAsync(lead.Email, subject, body, pdfBytes, $"{document.Number}.pdf", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send document {DocumentId} to {Email}.", id, lead.Email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Couldn't send the email. Try again in a moment." });
        }

        // A resend always advances SentAtUtc -- see DocumentRepository.UpdateStatusAsync.
        var now = DateTime.UtcNow;
        await _documentRepository.UpdateStatusAsync(id, "Sent", now, null);
        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = document.LeadId,
            Type = "DocumentSent",
            Content = $"{document.Type} {document.Number} sent to {lead.Email}",
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = now
        });

        return Ok(new { status = "Sent", sentAtUtc = now });
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        if (document.Type != "Invoice")
        {
            return BadRequest(new { message = "Only invoices can be marked as paid." });
        }
        if (document.Status != "Sent")
        {
            return BadRequest(new { message = "Send this invoice before marking it paid." });
        }

        var now = DateTime.UtcNow;
        await _documentRepository.UpdateStatusAsync(id, "Paid", null, now);
        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            LeadId = document.LeadId,
            Type = "DocumentPaid",
            Content = $"Invoice {document.Number} marked paid",
            CreatedByUserId = User.FindFirst("sub")?.Value,
            CreatedAtUtc = now
        });

        return Ok(new { status = "Paid", paidAtUtc = now });
    }

    // Mirrors OrganizationController.RequestSubscription's fallback for a downed Identity service.
    private async Task<OrganizationDetails> GetOrganizationOrFallbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _organizationClient.GetAsync(Request.Headers.Authorization.ToString(), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new OrganizationDetails(Guid.Empty, "(organization name unavailable)", null, null, null);
        }
    }
}
