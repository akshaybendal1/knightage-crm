-- The leads list is paginated with ORDER BY CreatedAtUtc DESC / OFFSET-FETCH
-- (LeadRepository.GetPagedAsync), but 006_performance_indexes.sql only added
-- indexes for PipelineStageId, LeadActivities.CreatedAtUtc, and Tasks --
-- missing the one column the default leads list sort actually depends on.
-- Without it, every page of the leads list is a full table scan + sort.

CREATE INDEX IX_Leads_CreatedAtUtc ON Leads(CreatedAtUtc DESC);
