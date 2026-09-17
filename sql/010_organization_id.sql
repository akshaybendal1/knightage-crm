-- Multi-tenancy: adds OrganizationId to every table so one shared CRM database can
-- serve all tenants, replacing the one-database-per-tenant scheme. Per-tenant
-- databases required CREATE DATABASE rights that prod's appuser login doesn't have
-- (and shouldn't need) -- knightage-identity already proves the shared-table pattern
-- works (Organizations/Users scoped by a plain OrganizationId column).
--
-- Safe to add these as plain NOT NULL columns only because this migration is applied
-- to a freshly created, still-empty Knightage_Crm database (see sql/onetime for the
-- one-time cutover that copies the existing tenant's data in afterward). Never run
-- this against a database that already has rows without adding a DEFAULT and a
-- backfill step first.

ALTER TABLE PipelineStages ADD OrganizationId UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE Leads ADD OrganizationId UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE LeadActivities ADD OrganizationId UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE Tasks ADD OrganizationId UNIQUEIDENTIFIER NOT NULL;

-- Existing indexes are dropped and replaced with OrganizationId leading, since it's
-- now the first filter in nearly every query -- SQL Server has no ALTER INDEX ...
-- ADD COLUMN, so narrower indexes are dropped outright rather than kept alongside a
-- new composite one. This isn't just an optimization: without an OrganizationId-
-- leading index, a query like "all tasks across the team" would scan every tenant's
-- rows, not just the caller's, once these tables are shared.

DROP INDEX IX_Leads_PipelineStageId ON Leads;
DROP INDEX IX_Leads_CreatedAtUtc ON Leads;
DROP INDEX IX_Tasks_AssignedToUserId_Status ON Tasks;
DROP INDEX IX_Tasks_Status_DueDate ON Tasks;
DROP INDEX IX_LeadActivities_LeadId ON LeadActivities;
DROP INDEX IX_LeadActivities_CreatedAtUtc ON LeadActivities;

CREATE INDEX IX_Leads_OrganizationId_PipelineStageId ON Leads(OrganizationId, PipelineStageId);
CREATE INDEX IX_Leads_OrganizationId_CreatedAtUtc ON Leads(OrganizationId, CreatedAtUtc DESC);
CREATE INDEX IX_Tasks_OrganizationId_AssignedToUserId_Status ON Tasks(OrganizationId, AssignedToUserId, Status);
CREATE INDEX IX_Tasks_OrganizationId_Status_DueDate ON Tasks(OrganizationId, Status, DueDate);
CREATE INDEX IX_LeadActivities_OrganizationId_LeadId ON LeadActivities(OrganizationId, LeadId);
CREATE INDEX IX_LeadActivities_OrganizationId_CreatedAtUtc ON LeadActivities(OrganizationId, CreatedAtUtc);

-- New: PipelineStages had no index beyond its PK before. GetActiveAsync's
-- WHERE IsActive=1 ORDER BY SortOrder would otherwise scan every tenant's stages.
CREATE INDEX IX_PipelineStages_OrganizationId_IsActive ON PipelineStages(OrganizationId, IsActive, SortOrder);

-- IX_Tasks_LeadId (from 004_tasks.sql) is deliberately left as-is, not made
-- composite: a single Lead's tasks already belong to exactly one org, so LeadId
-- alone stays highly selective regardless of how many tenants share the table.
