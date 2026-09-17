-- Backs the Sales Dashboard's admin-only coverage map/area view: every one of a
-- single rep's sales-entry locations within a date range, unpaged (unlike
-- usp_Leads_GetFieldSales, a single result set, no COUNT).
CREATE OR ALTER PROCEDURE usp_Leads_GetFieldSalesLocations
    @OrganizationId UNIQUEIDENTIFIER,
    @OwnerUserId NVARCHAR(100),
    @FromUtc DATETIME2,
    @ToUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT l.Id, l.Name, l.Email, l.Phone, l.Notes, l.OwnerUserId, l.CreatedAtUtc,
           l.Latitude, l.Longitude, l.LocationCapturedAtUtc,
           COALESCE(lp.TotalCost, 0) AS TotalCost,
           prod.Products
    FROM Leads l WITH (NOLOCK)
    LEFT JOIN (
        SELECT LeadId, SUM(Quantity * UnitPrice) AS TotalCost
        FROM LeadProducts WITH (NOLOCK)
        WHERE OrganizationId = @OrganizationId
        GROUP BY LeadId
    ) lp ON lp.LeadId = l.Id
    OUTER APPLY (
        SELECT STRING_AGG(p.Name + ' (' + ps.Name + ')', ', ') AS Products
        FROM LeadProducts lpx WITH (NOLOCK)
        JOIN Products p WITH (NOLOCK) ON p.Id = lpx.ProductId
        LEFT JOIN ProductStages ps WITH (NOLOCK) ON ps.Id = lpx.ProductStageId
        WHERE lpx.LeadId = l.Id
    ) prod
    WHERE l.OrganizationId = @OrganizationId AND l.Source = 'FieldSales'
      AND l.OwnerUserId = @OwnerUserId
      AND l.Latitude IS NOT NULL AND l.Longitude IS NOT NULL
      AND l.CreatedAtUtc >= @FromUtc AND l.CreatedAtUtc < @ToUtc
    ORDER BY l.CreatedAtUtc ASC;
END
GO
