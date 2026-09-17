-- OrganizationId baked in from creation (this table postdates multi-tenancy, so it
-- doesn't need the older create-then-ALTER-later two-step Leads/Tasks went through --
-- matches 012_products.sql's pattern instead).
--
-- GoogleSyncStatus is kept separate from "GoogleEventId IS NULL" because null alone
-- can't distinguish "owner never connected Calendar" from "owner connected, but this
-- specific push failed" -- the UI needs to tell those two apart.

CREATE TABLE Meetings (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    LeadId UNIQUEIDENTIFIER NOT NULL REFERENCES Leads(Id),
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000) NULL,
    StartAtUtc DATETIME2 NOT NULL,
    EndAtUtc DATETIME2 NOT NULL,
    Location NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Scheduled',          -- Scheduled | Cancelled | Completed
    OwnerUserId NVARCHAR(100) NOT NULL,                         -- whose Google Calendar this syncs to
    GoogleEventId NVARCHAR(1024) NULL,
    GoogleSyncStatus NVARCHAR(20) NOT NULL DEFAULT 'NotSynced', -- NotSynced | Synced | Failed | ReauthRequired
    GoogleSyncErrorAtUtc DATETIME2 NULL,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    UpdatedByUserId NVARCHAR(100) NULL,
    UpdatedAtUtc DATETIME2 NULL,
    CancelledAtUtc DATETIME2 NULL
);

CREATE INDEX IX_Meetings_OrganizationId_LeadId ON Meetings(OrganizationId, LeadId);
CREATE INDEX IX_Meetings_OrganizationId_OwnerUserId_StartAtUtc ON Meetings(OrganizationId, OwnerUserId, StartAtUtc);
