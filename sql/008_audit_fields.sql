-- Tracks who created/last modified a Lead, and who last modified a Task.
-- Tasks.CreatedByUserId and LeadActivities.CreatedByUserId already exist --
-- this fills the remaining gaps: Leads had no creator/modifier tracking at
-- all, and Tasks had no modifier tracking (only creator).

ALTER TABLE Leads ADD CreatedByUserId NVARCHAR(100) NULL;
ALTER TABLE Leads ADD UpdatedByUserId NVARCHAR(100) NULL;
ALTER TABLE Leads ADD UpdatedAtUtc DATETIME2 NULL;

ALTER TABLE Tasks ADD UpdatedByUserId NVARCHAR(100) NULL;
ALTER TABLE Tasks ADD UpdatedAtUtc DATETIME2 NULL;
