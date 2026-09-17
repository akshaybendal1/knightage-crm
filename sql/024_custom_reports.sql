-- Saves a user-built custom report definition (dimensions, view type, filters) as
-- opaque JSON -- CustomReportRepository.RunAsync only ever replays it back into the
-- same whitelisted query builder, it's never queried/filtered at the DB level, so
-- there's no reason to model it relationally.
CREATE TABLE SavedCustomReports (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    DefinitionJson NVARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    UpdatedByUserId NVARCHAR(100) NULL,
    UpdatedAtUtc DATETIME2 NULL
);
CREATE INDEX IX_SavedCustomReports_OrganizationId_IsActive ON SavedCustomReports(OrganizationId, IsActive, Name);
GO
