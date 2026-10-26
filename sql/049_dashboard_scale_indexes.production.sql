-- Every Dashboard widget (PipelineSummary, WonLostCounts, StuckLeadsCount) independently
-- scans this org's *entire* active-Leads set filtered on OrganizationId+IsActive, then
-- does per-row key lookups into the wide Leads row (Notes NVARCHAR(1000), etc.) just to
-- read PipelineStageId/PipelineStageChangedAtUtc/CreatedAtUtc. Neither existing index
-- (OrganizationId+CreatedAtUtc, OrganizationId+PipelineStageId) covers all three columns
-- together, so every dashboard load pays for wide row lookups proportional to lead count --
-- fine at a few thousand leads per org, increasingly costly as that grows toward ~100k.
-- This index lets all three widgets satisfy their scan from the index alone.
CREATE INDEX IX_Leads_OrganizationId_IsActive_Dashboard
    ON appuser.Leads(OrganizationId, IsActive)
    INCLUDE (PipelineStageId, PipelineStageChangedAtUtc, CreatedAtUtc);

-- Same reasoning for PipelineSummary's per-stage cost SUM and StuckLeadsCount's product-
-- stage branch, both of which join Leads to LeadProducts on OrganizationId and then need
-- LeadId/ProductStageId/StatusChangedAtUtc/Quantity/UnitPrice -- none of which the existing
-- (OrganizationId, LeadId) / (OrganizationId, ProductId) indexes carry as included columns.
CREATE INDEX IX_LeadProducts_OrganizationId_Dashboard
    ON appuser.LeadProducts(OrganizationId)
    INCLUDE (LeadId, ProductStageId, StatusChangedAtUtc, Quantity, UnitPrice);
GO

-- Two fixes here, found by isolating each branch and timing it separately against a
-- 100k-lead benchmark tenant (each branch alone: well under a second; the combined query
-- below as it stood before this migration: ~4m50s -- a cliff, not a gradual slowdown):
--
-- 1. COALESCE(PipelineStageChangedAtUtc, CreatedAtUtc) < @Cutoff (and the LeadProducts
--    equivalent) is rewritten as an equivalent OR -- COALESCE wraps the column in an
--    expression, which is never sargable, forcing a row-by-row evaluation instead of a
--    direct comparison against the stored value even when scanning the new covering index.
-- 2. UNION (distinct) between the two branches was the real cost, not either branch itself:
--    deduplicating two ~50k-row sets forces a sort/hash over the combined set, and on this
--    workload that dedup step was the ~4m50s. UNION ALL + COUNT(DISTINCT Id) produces the
--    exact same distinct-lead count without that combined-set sort -- confirmed identical
--    result (83,133 stuck leads either way) at 541ms instead of 290,533ms.
CREATE OR ALTER PROCEDURE appuser.usp_Dashboard_GetStuckLeadsCount
    @OrganizationId UNIQUEIDENTIFIER,
    @Cutoff DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(DISTINCT Id) FROM (
        SELECT l.Id FROM appuser.Leads l WITH (NOLOCK)
        JOIN appuser.PipelineStages ps WITH (NOLOCK) ON ps.Id = l.PipelineStageId
        WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1
          AND ps.IsWon = 0 AND ps.IsLost = 0
          AND (
                (l.PipelineStageChangedAtUtc IS NOT NULL AND l.PipelineStageChangedAtUtc < @Cutoff)
             OR (l.PipelineStageChangedAtUtc IS NULL AND l.CreatedAtUtc < @Cutoff)
          )
        UNION ALL
        SELECT lp.LeadId AS Id FROM appuser.LeadProducts lp WITH (NOLOCK)
        JOIN appuser.ProductStages pst WITH (NOLOCK) ON pst.Id = lp.ProductStageId AND pst.OrganizationId = @OrganizationId
        WHERE lp.OrganizationId = @OrganizationId
          AND pst.IsWon = 0 AND pst.IsLost = 0
          AND (
                (lp.StatusChangedAtUtc IS NOT NULL AND lp.StatusChangedAtUtc < @Cutoff)
             OR (lp.StatusChangedAtUtc IS NULL AND lp.CreatedAtUtc < @Cutoff)
          )
          AND EXISTS (SELECT 1 FROM appuser.Leads l2 WITH (NOLOCK) WHERE l2.Id = lp.LeadId AND l2.IsActive = 1)
    ) AS stuck;
END
GO
