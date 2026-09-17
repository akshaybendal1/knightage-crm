-- The dashboard's pipeline-summary query runs a correlated COUNT(*) subquery against Leads
-- per pipeline stage; without an index on the join/filter column that's a full table scan of
-- Leads once per stage. Likewise LeadActivities.CreatedAtUtc (activity-summary's date filter)
-- and Tasks(Status, DueDate) (overdue-tasks' filter) had no supporting index -- the existing
-- Tasks index leads with AssignedToUserId, which doesn't help a query that filters on
-- Status/DueDate without it.
CREATE INDEX IX_Leads_PipelineStageId ON Leads(PipelineStageId);
CREATE INDEX IX_LeadActivities_CreatedAtUtc ON LeadActivities(CreatedAtUtc);
CREATE INDEX IX_Tasks_Status_DueDate ON Tasks(Status, DueDate);
