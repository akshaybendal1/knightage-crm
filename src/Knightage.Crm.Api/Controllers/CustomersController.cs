using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Customers;
using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

/// <summary>
/// Customers are made when a lead is won (see LeadsController / SalesEntriesController) or added from the
/// field sales form. Listing and adding are open to every signed-in role because the field sales form
/// (which a Sales rep uses) needs both; the two bulk actions are Owner/Admin only.
/// </summary>
[Authorize]
[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repository;
    private readonly ICustomerService _service;
    private readonly IInventoryCustomerSync _inventorySync;

    public CustomersController(ICustomerRepository repository, ICustomerService service, IInventoryCustomerSync inventorySync)
    {
        _repository = repository;
        _service = service;
        _inventorySync = inventorySync;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        // Customers added in the Inventory portal join the list here (throttled, never fails the request).
        await _inventorySync.PullNewAsync(Request.Headers.Authorization.ToString(), cancellationToken);
        var customers = await _repository.GetAllAsync();
        var pendingInventory = customers.Count(c => c.IsActive && c.InventoryCustomerId is null);
        var wonLeadsWithoutCustomer = await _repository.CountWonLeadsWithoutCustomerAsync();
        return Ok(new { items = customers, pendingInventory, wonLeadsWithoutCustomer });
    }

    // Type-ahead for the New lead form and the lead page: a handful of matches, never the whole list.
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 8)
    {
        return Ok(await _repository.SearchAsync(q, limit));
    }

    // Brings customers added in the Inventory portal into the CRM's list (throttled to one attempt every couple of minutes per
    // organization; never fails the request). The field sales and New lead forms call it once when they open: they search the
    // customer list instead of loading all of it, and loading all of it was what used to trigger this as a side effect.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var added = await _inventorySync.PullNewAsync(Request.Headers.Authorization.ToString(), cancellationToken);
        return Ok(new { added });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerRequest request, CancellationToken cancellationToken)
    {
        var draft = CustomerDraft.Create(request.Name, request.Company, request.Email, request.Phone);
        if (draft is null)
        {
            return BadRequest(new { message = "Enter the customer's name." });
        }

        var result = await _service.AddAsync(draft, User.FindFirst("sub")?.Value, Request.Headers.Authorization.ToString(), cancellationToken);
        var response = new CustomerSaveResponse(result.Customer, result.AlreadyExisted);
        return result.AlreadyExisted ? Ok(response) : StatusCode(StatusCodes.Status201Created, response);
    }

    // Turns leads that were already won before customers existed into customers. Safe to run again: a lead
    // that already has a customer is skipped, and someone who is already a customer is linked, not duplicated.
    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("import-won-leads")]
    public async Task<IActionResult> ImportWonLeads(CancellationToken cancellationToken)
    {
        var result = await _service.ImportWonLeadsAsync(User.FindFirst("sub")?.Value, Request.Headers.Authorization.ToString(), cancellationToken);
        return Ok(result);
    }

    // The won leads that are not a customer yet, one page at a time, for picking which ones to convert.
    [Authorize(Roles = "Owner,Admin")]
    [HttpGet("won-leads")]
    public async Task<IActionResult> GetWonLeads([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await _repository.GetWonLeadsWithoutCustomerPageAsync(string.IsNullOrWhiteSpace(search) ? null : search.Trim(), page, pageSize);
        return Ok(new { items, totalCount, page, pageSize });
    }

    // Converts only the won leads the admin ticked.
    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("convert-won-leads")]
    public async Task<IActionResult> ConvertWonLeads(ConvertWonLeadsRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ConvertSelectedWonLeadsAsync(request.LeadIds, User.FindFirst("sub")?.Value, Request.Headers.Authorization.ToString(), cancellationToken);
        return Ok(result);
    }

    // Sends customers the Inventory portal doesn't have yet. Ignores the short "it just failed" pause that
    // automatic sends respect, because an admin pressing this wants a real attempt now.
    [Authorize(Roles = "Owner,Admin")]
    [HttpPost("sync-inventory")]
    public async Task<IActionResult> SyncInventory(CancellationToken cancellationToken)
    {
        var result = await _inventorySync.PushPendingAsync(Request.Headers.Authorization.ToString(), force: true, cancellationToken);
        return Ok(result);
    }
}
