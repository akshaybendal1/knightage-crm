-- Extends usp_Leads_GetFieldSales (041_stored_procs_sales_entries.production.sql, last
-- touched by 044_sales_entries_product_status.production.sql) to also select Company --
-- it was added to the Sales Entry form/Create endpoint without updating this read path,
-- so a logged Company never showed up on the rep's own entries list or the admin Sales
-- Dashboard.
CREATE OR ALTER PROCEDURE appuser.usp_Leads_GetFieldSales
    @OrganizationId UNIQUEIDENTIFIER,
    @OwnerUserId NVARCHAR(100) = NULL,
    @Offset INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM appuser.Leads l WITH (NOLOCK)
    WHERE l.OrganizationId = @OrganizationId AND l.Source = 'FieldSales'
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId);

    SELECT l.Id, l.Name, l.Company, l.Email, l.Phone, l.Notes, l.OwnerUserId, l.CreatedAtUtc,
           l.Latitude, l.Longitude, l.LocationCapturedAtUtc,
           COALESCE(lp.TotalCost, 0) AS TotalCost,
           prod.Products
    FROM appuser.Leads l WITH (NOLOCK)
    LEFT JOIN (
        SELECT LeadId, SUM(Quantity * UnitPrice) AS TotalCost
        FROM appuser.LeadProducts WITH (NOLOCK)
        WHERE OrganizationId = @OrganizationId
        GROUP BY LeadId
    ) lp ON lp.LeadId = l.Id
    OUTER APPLY (
        SELECT STRING_AGG(p.Name + ' (' + ps.Name + ')', ', ') AS Products
        FROM appuser.LeadProducts lpx WITH (NOLOCK)
        JOIN appuser.Products p WITH (NOLOCK) ON p.Id = lpx.ProductId
        LEFT JOIN appuser.ProductStages ps WITH (NOLOCK) ON ps.Id = lpx.ProductStageId
        WHERE lpx.LeadId = l.Id
    ) prod
    WHERE l.OrganizationId = @OrganizationId AND l.Source = 'FieldSales'
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
    ORDER BY l.CreatedAtUtc DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
