using System.Net.Http.Json;
using Knightage.Crm.Core.Interfaces;

namespace Knightage.Crm.Infrastructure.ExternalServices;

// Mirrors IdentityOrganizationClient's forwarded-auth pattern exactly. Uses the
// "IdentityCalendar" named HttpClient (a shorter, dedicated timeout -- see
// Program.cs) rather than the general "Identity" client, since this call chain is
// CRM -> Identity -> Google and must never hang a user's "save meeting" click.
public class IdentityCalendarClient : IIdentityCalendarClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    private record CalendarEventSyncResponse(string Outcome, string? GoogleEventId);

    public IdentityCalendarClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CalendarSyncResult> CreateEventAsync(string ownerUserId, CalendarEventRequest request, string? authorizationHeader, CancellationToken ct = default) =>
        PostAsync("api/calendar/events", new { ownerUserId, request.Title, request.Description, request.StartAtUtc, request.EndAtUtc, request.Location, request.AttendeeEmail }, authorizationHeader, ct);

    public Task<CalendarSyncResult> UpdateEventAsync(string ownerUserId, string googleEventId, CalendarEventRequest request, string? authorizationHeader, CancellationToken ct = default) =>
        PostAsync($"api/calendar/events/{Uri.EscapeDataString(googleEventId)}", new { ownerUserId, request.Title, request.Description, request.StartAtUtc, request.EndAtUtc, request.Location, request.AttendeeEmail }, authorizationHeader, ct);

    public Task<CalendarSyncResult> CancelEventAsync(string ownerUserId, string googleEventId, string? authorizationHeader, CancellationToken ct = default) =>
        PostAsync($"api/calendar/events/{Uri.EscapeDataString(googleEventId)}/cancel", new { ownerUserId }, authorizationHeader, ct);

    private async Task<CalendarSyncResult> PostAsync(string path, object body, string? authorizationHeader, CancellationToken ct)
    {
        try
        {
            var client = CreateClient(authorizationHeader);
            var response = await client.PostAsJsonAsync(path, body, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Includes the 403 cross-tenant case -- from CRM's side there's nothing
                // to do differently than any other sync failure, so it collapses to Failed.
                return new CalendarSyncResult(CalendarSyncOutcome.Failed, null);
            }

            var result = await response.Content.ReadFromJsonAsync<CalendarEventSyncResponse>(cancellationToken: ct);
            if (result is null || !Enum.TryParse<CalendarSyncOutcome>(result.Outcome, out var outcome))
            {
                return new CalendarSyncResult(CalendarSyncOutcome.Failed, null);
            }

            return new CalendarSyncResult(outcome, result.GoogleEventId);
        }
        catch (Exception)
        {
            // Timeout, network failure, malformed response -- this client never throws
            // outward, since a Calendar sync failure must never surface as a failure of
            // the underlying meeting create/update/cancel request.
            return new CalendarSyncResult(CalendarSyncOutcome.Failed, null);
        }
    }

    private HttpClient CreateClient(string? authorizationHeader)
    {
        var client = _httpClientFactory.CreateClient("IdentityCalendar");
        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationHeader);
        }
        return client;
    }
}
