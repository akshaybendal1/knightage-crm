-- Schema-qualified equivalent of 024_custom_reports.sql for production. Unlike
-- 021_product_stages.sql (also a brand-new table), an unqualified CREATE TABLE run
-- through the deploy CLI's --db-only path lands under the CONNECTING DB USER's own
-- default schema (appuser_ab), not `appuser` where every real application table
-- actually lives -- confirmed directly: running 024_custom_reports.sql as-is against
-- production created appuser_ab.SavedCustomReports, which the app (connecting as
-- appuser) can never see. That empty, unused table was left in place rather than
-- dropped (a destructive action against production, not taken lightly) -- this
-- script creates the real, correctly-schemed table the application actually uses.
CREATE TABLE appuser.SavedCustomReports (
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
CREATE INDEX IX_SavedCustomReports_OrganizationId_IsActive ON appuser.SavedCustomReports(OrganizationId, IsActive, Name);
GO
