-- Backs the Sales Dashboard's "Sales by rep" chart: org-wide per-rep entry
-- count + total value for FieldSales leads in a date range. Mirrors
-- usp_Reports_GetLeadsByOwnerRaw's ISNULL/GROUP BY/Since-Until shape
-- (036_stored_procs_dashboard_reports.sql) plus the LeadProducts value join
-- already used by usp_Leads_GetFieldSalesLocations.
CREATE OR ALTER PROCEDURE appuser.usp_Leads_GetFieldSalesByRep
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ISNULL(l.OwnerUserId, '') AS OwnerUserId,
           COUNT(*) AS EntryCount,
           COALESCE(SUM(lp.TotalCost), 0) AS TotalValue
    FROM appuser.Leads l WITH (NOLOCK)
    LEFT JOIN (
        SELECT LeadId, SUM(Quantity * UnitPrice) AS TotalCost
        FROM appuser.LeadProducts WITH (NOLOCK)
        WHERE OrganizationId = @OrganizationId
        GROUP BY LeadId
    ) lp ON lp.LeadId = l.Id
    WHERE l.OrganizationId = @OrganizationId AND l.Source = 'FieldSales'
      AND (@Since IS NULL OR l.CreatedAtUtc >= @Since)
      AND (@Until IS NULL OR l.CreatedAtUtc < @Until)
    GROUP BY l.OwnerUserId;
END
GO
