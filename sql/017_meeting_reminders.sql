-- Tracks whether a "starting soon" digest email has already been sent for a meeting,
-- so the reminder check (run opportunistically from TenantResolutionMiddleware, see
-- IReminderDigestService) never double-sends. Mirrors 007_task_reminders.sql.
ALTER TABLE Meetings ADD ReminderSentAtUtc DATETIME2 NULL;
