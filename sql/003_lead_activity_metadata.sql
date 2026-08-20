-- Adds structured metadata to activity entries (e.g. stage-change from/to names),
-- needed now that activities are auto-logged by the server, not just typed by a
-- user. Free text (Content) alone can't carry structured fields cleanly.

ALTER TABLE LeadActivities ADD Metadata NVARCHAR(MAX) NULL;
