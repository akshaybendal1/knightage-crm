namespace Knightage.Crm.Core.Interfaces;

public enum CalendarSyncOutcome { Synced, NotConnected, ReauthRequired, Failed }

public record CalendarSyncResult(CalendarSyncOutcome Outcome, string? GoogleEventId);

public record CalendarEventRequest(string Title, string? Description, DateTime StartAtUtc, DateTime EndAtUtc, string? Location, string? AttendeeEmail);

/// <summary>
/// Talks to knightage-identity's narrow "act on my connected calendar" API using the
/// caller's own forwarded bearer token -- same pattern as IIdentityOrganizationClient.
/// CRM never sees a Google refresh token, only these event-shaped DTOs.
///
/// ownerUserId is always an explicit parameter, never derived from the caller's own
/// token: the person scheduling a meeting and the meeting's owner are frequently
/// different people (I schedule it, a teammate owns it) -- the forwarded token
/// authenticates who's calling, ownerUserId (read off the Meeting row) says whose
/// calendar to touch. This client itself never throws outward; any failure (non-2xx,
/// timeout, bad deserialize) maps to a Failed outcome.
/// </summary>
public interface IIdentityCalendarClient
{
    Task<CalendarSyncResult> CreateEventAsync(string ownerUserId, CalendarEventRequest request, string? authorizationHeader, CancellationToken ct = default);
    Task<CalendarSyncResult> UpdateEventAsync(string ownerUserId, string googleEventId, CalendarEventRequest request, string? authorizationHeader, CancellationToken ct = default);
    Task<CalendarSyncResult> CancelEventAsync(string ownerUserId, string googleEventId, string? authorizationHeader, CancellationToken ct = default);
}
