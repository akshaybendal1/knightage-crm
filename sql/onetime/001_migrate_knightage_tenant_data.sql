-- ONE-TIME cutover script. Copies the "Knightage" tenant's data from its old
-- per-tenant database into the new shared Knightage_Crm database, tagging every row
-- with OrganizationId. Run exactly once against production, after Knightage_Crm has
-- been created and sql/001_init.sql .. sql/010_organization_id.sql have been applied
-- to it, during the maintenance window (app_offline.htm up) immediately before
-- deploying the new CRM backend code. Both databases must be on the same SQL Server
-- instance -- same-instance cross-database queries via three-part naming.
--
-- Schema note: Knightage_Crm_Knightage's tables live under dbo (created via the app's
-- own CREATE DATABASE + schema run, which made appuser the db_owner there). The new
-- Knightage_Crm database was created via a different path and appuser's default
-- schema in it is "appuser", not dbo -- confirmed by checking both directly. The app
-- itself connects as appuser, so unqualified queries at runtime resolve to
-- Knightage_Crm.appuser.* correctly; this script targets that schema explicitly.

DECLARE @OrgId UNIQUEIDENTIFIER = '578bceef-8dc7-4d8e-a1e4-39425a663841';

-- 1. PipelineStages first -- Leads.PipelineStageId references it.
INSERT INTO Knightage_Crm.appuser.PipelineStages (Id, OrganizationId, Name, SortOrder, IsActive, IsWon, IsLost)
SELECT Id, @OrgId, Name, SortOrder, IsActive, IsWon, IsLost
FROM Knightage_Crm_Knightage.dbo.PipelineStages;

-- 2. Leads next -- LeadActivities.LeadId and Tasks.LeadId reference it.
INSERT INTO Knightage_Crm.appuser.Leads
    (Id, OrganizationId, Name, Email, Phone, Company, PipelineStageId, Notes, Source,
     CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc)
SELECT Id, @OrgId, Name, Email, Phone, Company, PipelineStageId, Notes, Source,
       CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc
FROM Knightage_Crm_Knightage.dbo.Leads;

-- 3. LeadActivities and Tasks both only depend on Leads -- order between them doesn't matter.
INSERT INTO Knightage_Crm.appuser.LeadActivities (Id, OrganizationId, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc)
SELECT Id, @OrgId, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc
FROM Knightage_Crm_Knightage.dbo.LeadActivities;

INSERT INTO Knightage_Crm.appuser.Tasks
    (Id, OrganizationId, LeadId, Title, Description, DueDate, Status, AssignedToUserId,
     CreatedByUserId, CreatedAtUtc, CompletedAtUtc, ReminderSentAtUtc, UpdatedByUserId, UpdatedAtUtc)
SELECT Id, @OrgId, LeadId, Title, Description, DueDate, Status, AssignedToUserId,
       CreatedByUserId, CreatedAtUtc, CompletedAtUtc, ReminderSentAtUtc, UpdatedByUserId, UpdatedAtUtc
FROM Knightage_Crm_Knightage.dbo.Tasks;

-- 4. Verify row counts match exactly before proceeding to deploy new code.
SELECT 'PipelineStages' AS TableName,
       (SELECT COUNT(*) FROM Knightage_Crm_Knightage.dbo.PipelineStages) AS SourceCount,
       (SELECT COUNT(*) FROM Knightage_Crm.appuser.PipelineStages WHERE OrganizationId = @OrgId) AS TargetCount
UNION ALL
SELECT 'Leads', (SELECT COUNT(*) FROM Knightage_Crm_Knightage.dbo.Leads), (SELECT COUNT(*) FROM Knightage_Crm.appuser.Leads WHERE OrganizationId = @OrgId)
UNION ALL
SELECT 'LeadActivities', (SELECT COUNT(*) FROM Knightage_Crm_Knightage.dbo.LeadActivities), (SELECT COUNT(*) FROM Knightage_Crm.appuser.LeadActivities WHERE OrganizationId = @OrgId)
UNION ALL
SELECT 'Tasks', (SELECT COUNT(*) FROM Knightage_Crm_Knightage.dbo.Tasks), (SELECT COUNT(*) FROM Knightage_Crm.appuser.Tasks WHERE OrganizationId = @OrgId);
