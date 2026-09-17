-- Dedicated stage-changed timestamp for Leads -- Lead.UpdatedAtUtc is generic
-- and gets clobbered by ANY field edit (LeadsController.Update sets it
-- unconditionally on every save), so it cannot be used to detect "stuck in
-- this stage too long." This column is only ever touched inside
-- LeadsController.Update's `if (previousStageId != request.PipelineStageId)`
-- branch, plus on Create/Import.

ALTER TABLE Leads ADD PipelineStageChangedAtUtc DATETIME2 NULL;
GO

-- Backfill: treat every existing lead as having entered its current stage at
-- creation -- there's no historical record of earlier stage changes before
-- this column existed.
UPDATE Leads SET PipelineStageChangedAtUtc = CreatedAtUtc WHERE PipelineStageChangedAtUtc IS NULL;
