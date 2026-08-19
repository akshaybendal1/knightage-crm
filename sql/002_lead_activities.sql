-- Adds the activity timeline for a lead: notes today, the same table can carry
-- future activity types (call logs, emails, task completions, stage-change
-- entries) once those slices land, without a new table per type.

CREATE TABLE LeadActivities (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    LeadId UNIQUEIDENTIFIER NOT NULL REFERENCES Leads(Id),
    Type NVARCHAR(50) NOT NULL DEFAULT 'Note',
    Content NVARCHAR(2000) NOT NULL,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_LeadActivities_LeadId ON LeadActivities(LeadId);
