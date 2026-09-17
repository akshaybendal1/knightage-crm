-- Tracks whether a "due tomorrow" digest email has already been sent for a task,
-- so the reminder check (run opportunistically from TenantResolutionMiddleware,
-- see ITaskReminderDigestService) never double-sends.
ALTER TABLE Tasks ADD ReminderSentAtUtc DATETIME2 NULL;
