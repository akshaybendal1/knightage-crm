using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Service;

public class ReminderDigestService : IReminderDigestService
{
    private readonly ILeadTaskRepository _taskRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IIdentityUserDirectory _identityDirectory;
    private readonly IEmailSender _emailSender;
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly IPushSender _pushSender;
    private readonly ReminderDigestGate _gate;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReminderDigestService> _logger;

    public ReminderDigestService(
        ILeadTaskRepository taskRepository,
        IMeetingRepository meetingRepository,
        IIdentityUserDirectory identityDirectory,
        IEmailSender emailSender,
        IDeviceTokenRepository deviceTokenRepository,
        IPushSender pushSender,
        ReminderDigestGate gate,
        IConfiguration configuration,
        ILogger<ReminderDigestService> logger)
    {
        _taskRepository = taskRepository;
        _meetingRepository = meetingRepository;
        _identityDirectory = identityDirectory;
        _emailSender = emailSender;
        _deviceTokenRepository = deviceTokenRepository;
        _pushSender = pushSender;
        _gate = gate;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunIfDueAsync(Guid organizationId, string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        // Claimed atomically, upfront, before any work starts -- this is both the "should
        // I run" check and the "mark attempted" step in one, so a burst of near-simultaneous
        // requests (a single page load commonly fires several) can never let more than one
        // of them through for the same org on the same day. See ReminderDigestGate for why
        // this must be atomic rather than a separate check-then-write.
        if (!_gate.TryClaim(organizationId))
        {
            return;
        }

        try
        {
            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            var dueTasks = await _taskRepository.GetDueTomorrowUnremindedAsync(tomorrow);
            var dueMeetings = await _meetingRepository.GetStartingSoonUnremindedAsync(DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
            if (dueTasks.Count == 0 && dueMeetings.Count == 0)
            {
                return;
            }

            var users = (await _identityDirectory.GetOrganizationUsersAsync(authorizationHeader, cancellationToken))
                .ToDictionary(u => u.Id, u => u);

            var taskGroups = dueTasks.GroupBy(t => t.AssignedToUserId).ToDictionary(g => g.Key, g => g.ToList());
            var meetingGroups = dueMeetings.GroupBy(m => m.OwnerUserId).ToDictionary(g => g.Key, g => g.ToList());

            // Fetched once up front, same as the user-directory lookup above, rather than
            // per user in the loop below.
            var tokensByUser = (await _deviceTokenRepository.GetTokensForUsersAsync(taskGroups.Keys.Union(meetingGroups.Keys)))
                .GroupBy(t => t.UserId)
                .ToDictionary(g => g.Key, g => g.Select(t => t.Token).ToList());

            // A user might have only tasks, only meetings, or both -- iterate the union so
            // nobody is skipped just because one of the two dictionaries has no entry for them.
            foreach (var userId in taskGroups.Keys.Union(meetingGroups.Keys))
            {
                if (!users.TryGetValue(userId, out var user) || string.IsNullOrWhiteSpace(user.Email))
                {
                    continue;
                }

                var tasksForUser = taskGroups.TryGetValue(userId, out var tasks) ? tasks : new List<LeadTask>();
                var meetingsForUser = meetingGroups.TryGetValue(userId, out var meetings) ? meetings : new List<Meeting>();

                try
                {
                    await _emailSender.SendAsync(
                        user.Email,
                        BuildSubject(tasksForUser, meetingsForUser),
                        BuildBody(user.DisplayName, tasksForUser, meetingsForUser),
                        cancellationToken);

                    // Own try/catch, separate from the email send above -- a push failure
                    // must never suppress the email-success path or the reminder-sent
                    // marking below, and vice versa; both channels simply retry together
                    // next day if either fails, matching the outer catch's existing retry
                    // semantics for a whole-org failure.
                    if (tokensByUser.TryGetValue(userId, out var tokens) && tokens.Count > 0)
                    {
                        try
                        {
                            await _pushSender.SendAsync(
                                tokens,
                                BuildSubject(tasksForUser, meetingsForUser),
                                BuildPushBody(tasksForUser, meetingsForUser),
                                BuildPushData(tasksForUser, meetingsForUser),
                                cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Reminder push failed for {UserId} in org {OrganizationId}.", userId, organizationId);
                        }
                    }

                    // Marked right after this user's own successful send, not in one final
                    // batch at the end -- a mid-loop SMTP hiccup can't cause a later retry to
                    // re-send to people who already got theirs this run. Guarded by .Any() so
                    // an empty IN () is never issued for whichever type this user didn't have.
                    if (tasksForUser.Count > 0)
                    {
                        await _taskRepository.MarkRemindedAsync(tasksForUser.Select(t => t.Id), DateTime.UtcNow);
                    }
                    if (meetingsForUser.Count > 0)
                    {
                        await _meetingRepository.MarkRemindedAsync(meetingsForUser.Select(m => m.Id), DateTime.UtcNow);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reminder email failed for {UserId} in org {OrganizationId}.", userId, organizationId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reminder digest failed for organization {OrganizationId}.", organizationId);
        }
    }

    // The queries behind these (GetDueTomorrowUnremindedAsync / GetStartingSoonUnremindedAsync)
    // deliberately use <=, not =, so a gap in when this check last ran (org idle a few days,
    // an outage) doesn't silently lose a reminder -- see the note on each. That means a single
    // digest can legitimately contain tasks that are actually overdue or due today (not just
    // tomorrow) and meetings that are already in progress, so the email must say so honestly
    // rather than blanket-labeling everything "tomorrow."
    private static string BuildSubject(List<LeadTask> tasks, List<Meeting> meetings)
    {
        var parts = new List<string>();
        if (tasks.Count > 0)
        {
            parts.Add($"{tasks.Count} task");
        }
        if (meetings.Count > 0)
        {
            parts.Add($"{meetings.Count} meeting");
        }
        var total = tasks.Count + meetings.Count;
        return $"{string.Join(", ", parts)} reminder{(total == 1 ? "" : "s")}";
    }

    private string BuildBody(string displayName, List<LeadTask> tasks, List<Meeting> meetings)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5105";
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var sections = new List<string>();

        if (tasks.Count > 0)
        {
            var overdue = tasks.Where(t => t.DueDate < today).ToList();
            var dueToday = tasks.Where(t => t.DueDate == today).ToList();
            var dueTomorrow = tasks.Where(t => t.DueDate > today).ToList();

            var taskSections = new List<string>();
            if (overdue.Count > 0)
            {
                taskSections.Add($"Overdue:\n{FormatTaskLines(overdue)}");
            }
            if (dueToday.Count > 0)
            {
                taskSections.Add($"Due today:\n{FormatTaskLines(dueToday)}");
            }
            if (dueTomorrow.Count > 0)
            {
                taskSections.Add($"Due tomorrow:\n{FormatTaskLines(dueTomorrow)}");
            }
            sections.Add($"Tasks:\n\n{string.Join("\n\n", taskSections)}");
        }

        if (meetings.Count > 0)
        {
            // In progress / Today / Tomorrow, not Overdue/Today/Tomorrow like Tasks --
            // GetStartingSoonUnremindedAsync's EndAtUtc >= now bound means a meeting can
            // only ever land here if it hasn't finished yet, so "in progress" is the
            // honest label for one that's already started.
            var inProgress = meetings.Where(m => m.StartAtUtc <= now).ToList();
            var startingToday = meetings.Where(m => m.StartAtUtc > now && m.StartAtUtc.Date == today).ToList();
            var startingTomorrow = meetings.Where(m => m.StartAtUtc > now && m.StartAtUtc.Date > today).ToList();

            var meetingSections = new List<string>();
            if (inProgress.Count > 0)
            {
                meetingSections.Add($"In progress:\n{FormatMeetingLines(inProgress)}");
            }
            if (startingToday.Count > 0)
            {
                meetingSections.Add($"Today:\n{FormatMeetingLines(startingToday)}");
            }
            if (startingTomorrow.Count > 0)
            {
                meetingSections.Add($"Tomorrow:\n{FormatMeetingLines(startingTomorrow)}");
            }
            sections.Add($"Meetings:\n\n{string.Join("\n\n", meetingSections)}");
        }

        var body = string.Join("\n\n", sections);
        var totalCount = tasks.Count + meetings.Count;

        return $"""
            Hi {displayName},

            You have {totalCount} reminder{(totalCount == 1 ? "" : "s")} that need attention:

            {body}

            View your dashboard: {baseUrl}/dashboard
            """;
    }

    private static string FormatTaskLines(IEnumerable<LeadTask> tasks) =>
        string.Join("\n", tasks.Select(t => $"- {t.Title} ({t.LeadName})"));

    private static string FormatMeetingLines(IEnumerable<Meeting> meetings) =>
        string.Join("\n", meetings.Select(m => $"- {m.Title} ({m.LeadName}) at {m.StartAtUtc:yyyy-MM-dd HH:mm} UTC"));

    // Terse, single-line version of BuildBody for a push notification's tray text --
    // the full multi-line/multi-section email body doesn't fit a notification.
    private static string BuildPushBody(List<LeadTask> tasks, List<Meeting> meetings)
    {
        var names = tasks.Select(t => t.Title).Concat(meetings.Select(m => m.Title)).ToList();
        var shown = string.Join(", ", names.Take(3));
        return names.Count > 3 ? $"{shown} +{names.Count - 3} more" : shown;
    }

    // Only safe to deep-link straight to a lead when every item in this digest belongs
    // to the same one -- a digest spanning several leads has no single correct target,
    // so the caller (the Android app) falls back to a generic screen when this is null.
    private static IReadOnlyDictionary<string, string>? BuildPushData(List<LeadTask> tasks, List<Meeting> meetings)
    {
        var leadIds = tasks.Select(t => t.LeadId).Concat(meetings.Select(m => m.LeadId)).Distinct().ToList();
        if (leadIds.Count != 1)
        {
            return null;
        }

        var type = meetings.Count > 0 && tasks.Count == 0 ? "meeting" : "task";
        return new Dictionary<string, string> { ["leadId"] = leadIds[0].ToString(), ["type"] = type };
    }
}
