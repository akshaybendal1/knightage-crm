-- Customizable per-org stages for LeadProduct, mirroring PipelineStages exactly --
-- replaces the old hardcoded Active/Won/Lost string with a real stage entity.
CREATE TABLE ProductStages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsWon BIT NOT NULL DEFAULT 0,
    IsLost BIT NOT NULL DEFAULT 0
);
CREATE INDEX IX_ProductStages_OrganizationId_IsActive ON ProductStages(OrganizationId, IsActive, SortOrder);
GO

-- One-time backfill only, NOT a general new-org seed mechanism (PipelineStages
-- itself is never auto-seeded either -- a brand new org starts empty and uses the
-- inline "+ Add new stage" quick-add). Every org that already has LeadProducts
-- rows gets the same three stages their old Status strings represented, so
-- existing data has somewhere valid to point before ProductStageId goes NOT NULL.
INSERT INTO ProductStages (Id, OrganizationId, Name, SortOrder, IsActive, IsWon, IsLost)
SELECT NEWID(), OrganizationId, 'Active', 1, 1, 0, 0 FROM (SELECT DISTINCT OrganizationId FROM LeadProducts) o;
INSERT INTO ProductStages (Id, OrganizationId, Name, SortOrder, IsActive, IsWon, IsLost)
SELECT NEWID(), OrganizationId, 'Won', 2, 1, 1, 0 FROM (SELECT DISTINCT OrganizationId FROM LeadProducts) o;
INSERT INTO ProductStages (Id, OrganizationId, Name, SortOrder, IsActive, IsWon, IsLost)
SELECT NEWID(), OrganizationId, 'Lost', 3, 1, 0, 1 FROM (SELECT DISTINCT OrganizationId FROM LeadProducts) o;
GO

ALTER TABLE LeadProducts ADD ProductStageId UNIQUEIDENTIFIER NULL;
GO
UPDATE lp SET lp.ProductStageId = ps.Id
FROM LeadProducts lp
JOIN ProductStages ps ON ps.OrganizationId = lp.OrganizationId AND ps.Name = lp.Status;
GO

-- Verify zero rows before running the rest of this file (both locally and in
-- production): SELECT COUNT(*) FROM LeadProducts WHERE ProductStageId IS NULL;
ALTER TABLE LeadProducts ALTER COLUMN ProductStageId UNIQUEIDENTIFIER NOT NULL;
GO
ALTER TABLE LeadProducts ADD CONSTRAINT FK_LeadProducts_ProductStages FOREIGN KEY (ProductStageId) REFERENCES ProductStages(Id);
GO

-- Status (migration 018) has an unnamed DEFAULT 'Active' constraint, whose
-- auto-generated name differs per database -- must be dropped by lookup before
-- the column itself can be dropped.
DECLARE @statusDefaultConstraint NVARCHAR(200);
SELECT @statusDefaultConstraint = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE dc.parent_object_id = OBJECT_ID('LeadProducts') AND c.name = 'Status';
IF @statusDefaultConstraint IS NOT NULL
    EXEC('ALTER TABLE LeadProducts DROP CONSTRAINT ' + @statusDefaultConstraint);
GO
ALTER TABLE LeadProducts DROP COLUMN Status;
GO

