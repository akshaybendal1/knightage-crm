-- Product-level status for a LeadProduct line item -- a fixed three-state set
-- (Active/Won/Lost), independent of the lead's own pipeline stage; not a reuse
-- of the org's customizable PipelineStages. StatusChangedAtUtc powers "stuck"
-- detection (see sibling migration 019_lead_pipeline_stage_changed_at.sql for
-- the Leads side) -- CreatedAtUtc can't be reused for that since it never
-- changes after insert, and a Quantity edit (LeadProductRepository.
-- UpdateQuantityAsync) must NOT reset this clock, so it's a dedicated column.

ALTER TABLE LeadProducts ADD Status NVARCHAR(20) NOT NULL DEFAULT 'Active';
ALTER TABLE LeadProducts ADD StatusChangedAtUtc DATETIME2 NULL;
GO

-- Backfill: every existing line item has implicitly been "Active" since
-- creation -- there's no real prior status-change instant, so CreatedAtUtc is
-- the honest starting point.
UPDATE LeadProducts SET StatusChangedAtUtc = CreatedAtUtc WHERE StatusChangedAtUtc IS NULL;
