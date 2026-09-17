using Knightage.Crm.Api.Contracts;
using Knightage.Crm.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knightage.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/meetings")]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly IIdentityCalendarClient _identityCalendarClient;

    public MeetingsController(IMeetingRepository meetingRepository, IIdentityCalendarClient identityCalendarClient)
    {
        _meetingRepository = meetingRepository;
        _identityCalendarClient = identityCalendarClient;
    }

    // Read-only "what's coming up for me" view (Dashboard's Upcoming reminders card) --
    // never touches ReminderSentAtUtc, unlike the digest that also finds starting-soon
    // meetings (ReminderDigestService uses a repository method of its own for that).
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] int hours = 24)
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized();
        }

        var clampedHours = Math.Clamp(hours, 1, 168);
        var now = DateTime.UtcNow;
        var meetings = await _meetingRepository.GetUpcomingForOwnerAsync(currentUserId, now, now.AddHours(clampedHours));
        return Ok(meetings);
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMeetingRequest request)
    {
        var existing = await _meetingRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        if (request.EndAtUtc <= request.StartAtUtc)
        {
            return BadRequest(new { message = "endAtUtc must be after startAtUtc." });
        }

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.StartAtUtc = request.StartAtUtc;
        existing.EndAtUtc = request.EndAtUtc;
        existing.Location = request.Location;
        existing.UpdatedByUserId = User.FindFirst("sub")?.Value;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await _meetingRepository.UpdateAsync(existing);

        // Retry-on-edit is the entire retry story for v1: if this meeting isn't currently
        // Synced, treat this edit as a fresh attempt to create the event rather than update
        // one that never existed on Google's side (an owner who just connected, or a prior
        // attempt that failed/was never connected, both fall into this branch).
        try
        {
            var authorizationHeader = Request.Headers.Authorization.ToString();
            var eventRequest = new CalendarEventRequest(existing.Title, existing.Description, existing.StartAtUtc, existing.EndAtUtc, existing.Location, null);
            var result = existing.GoogleSyncStatus == "Synced" && !string.IsNullOrEmpty(existing.GoogleEventId)
                ? await _identityCalendarClient.UpdateEventAsync(existing.OwnerUserId, existing.GoogleEventId, eventRequest, authorizationHeader)
                : await _identityCalendarClient.CreateEventAsync(existing.OwnerUserId, eventRequest, authorizationHeader);

            var syncStatus = MapSyncStatus(result.Outcome);
            await _meetingRepository.UpdateGoogleSyncAsync(id, result.GoogleEventId, syncStatus, result.Outcome == CalendarSyncOutcome.Failed ? DateTime.UtcNow : null);
            existing.GoogleEventId = result.GoogleEventId;
            existing.GoogleSyncStatus = syncStatus;
        }
        catch
        {
            await _meetingRepository.UpdateGoogleSyncAsync(id, existing.GoogleEventId, "Failed", DateTime.UtcNow);
            existing.GoogleSyncStatus = "Failed";
        }

        return Ok(existing);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateMeetingStatusRequest request)
    {
        if (request.Status is not ("Cancelled" or "Completed"))
        {
            return BadRequest(new { message = "status must be Cancelled or Completed." });
        }

        var existing = await _meetingRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirst("sub")?.Value;
        var updatedAtUtc = DateTime.UtcNow;
        var cancelledAtUtc = request.Status == "Cancelled" ? updatedAtUtc : (DateTime?)null;
        await _meetingRepository.UpdateStatusAsync(id, request.Status, cancelledAtUtc, currentUserId, updatedAtUtc);
        existing.Status = request.Status;
        existing.CancelledAtUtc = cancelledAtUtc;
        existing.UpdatedByUserId = currentUserId;
        existing.UpdatedAtUtc = updatedAtUtc;

        // Completing a meeting is pure in-app state -- Google Calendar has no "completed"
        // concept for a past event, so it never touches Google. Cancelling only calls out
        // if there's actually a Google event to cancel.
        if (request.Status == "Cancelled" && !string.IsNullOrEmpty(existing.GoogleEventId))
        {
            try
            {
                var authorizationHeader = Request.Headers.Authorization.ToString();
                var result = await _identityCalendarClient.CancelEventAsync(existing.OwnerUserId, existing.GoogleEventId, authorizationHeader);
                if (result.Outcome == CalendarSyncOutcome.Synced)
                {
                    // Synced here means "Google confirms the event is gone" -- clear the
                    // pointer so a future re-schedule of this same row (there isn't one in
                    // v1, but UpdateGoogleSyncAsync is the single place this is written)
                    // never mistakes a stale id for a live event.
                    await _meetingRepository.UpdateGoogleSyncAsync(id, null, "NotSynced", null);
                    existing.GoogleEventId = null;
                    existing.GoogleSyncStatus = "NotSynced";
                }
                else
                {
                    var syncStatus = MapSyncStatus(result.Outcome);
                    await _meetingRepository.UpdateGoogleSyncAsync(id, existing.GoogleEventId, syncStatus, result.Outcome == CalendarSyncOutcome.Failed ? DateTime.UtcNow : null);
                    existing.GoogleSyncStatus = syncStatus;
                }
            }
            catch
            {
                await _meetingRepository.UpdateGoogleSyncAsync(id, existing.GoogleEventId, "Failed", DateTime.UtcNow);
                existing.GoogleSyncStatus = "Failed";
            }
        }

        return Ok(existing);
    }

    private static string MapSyncStatus(CalendarSyncOutcome outcome) => outcome switch
    {
        CalendarSyncOutcome.Synced => "Synced",
        CalendarSyncOutcome.ReauthRequired => "ReauthRequired",
        CalendarSyncOutcome.Failed => "Failed",
        _ => "NotSynced"
    };
}
