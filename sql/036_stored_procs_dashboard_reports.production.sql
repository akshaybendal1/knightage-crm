CREATE PROCEDURE appuser.usp_Dashboard_GetPipelineSummary
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ps.Id AS StageId, ps.Name AS StageName, ps.SortOrder,
           (SELECT COUNT(*) FROM appuser.Leads l WITH (NOLOCK) WHERE l.PipelineStageId = ps.Id AND l.OrganizationId = @OrganizationId AND l.IsActive = 1) AS LeadCount,
           (SELECT COALESCE(SUM(lp.Quantity * lp.UnitPrice), 0)
            FROM appuser.Leads l WITH (NOLOCK) JOIN appuser.LeadProducts lp WITH (NOLOCK) ON lp.LeadId = l.Id AND lp.OrganizationId = @OrganizationId
            WHERE l.PipelineStageId = ps.Id AND l.OrganizationId = @OrganizationId AND l.IsActive = 1) AS LeadValue
    FROM appuser.PipelineStages ps WITH (NOLOCK)
    WHERE ps.OrganizationId = @OrganizationId AND ps.IsActive = 1
    ORDER BY ps.SortOrder;
END
GO

CREATE PROCEDURE appuser.usp_Dashboard_GetWonLostCounts
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM appuser.Leads l WITH (NOLOCK) JOIN appuser.PipelineStages ps WITH (NOLOCK) ON ps.Id = l.PipelineStageId
         WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND ps.IsWon = 1 AND (@Since IS NULL OR l.CreatedAtUtc >= @Since)
           AND (@Until IS NULL OR l.CreatedAtUtc < @Until)) AS Won,
        (SELECT COUNT(*) FROM appuser.Leads l WITH (NOLOCK) JOIN appuser.PipelineStages ps WITH (NOLOCK) ON ps.Id = l.PipelineStageId
         WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND ps.IsLost = 1 AND (@Since IS NULL OR l.CreatedAtUtc >= @Since)
           AND (@Until IS NULL OR l.CreatedAtUtc < @Until)) AS Lost;
END
GO

CREATE PROCEDURE appuser.usp_Dashboard_GetActivityCountSince
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM appuser.LeadActivities WITH (NOLOCK) WHERE OrganizationId = @OrganizationId AND CreatedAtUtc >= @Since;
END
GO

CREATE PROCEDURE appuser.usp_Dashboard_GetOverdueOpenTaskCount
    @OrganizationId UNIQUEIDENTIFIER,
    @Today DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM appuser.Tasks WITH (NOLOCK) WHERE OrganizationId = @OrganizationId AND Status = 'Open' AND DueDate < @Today;
END
GO

CREATE PROCEDURE appuser.usp_Dashboard_GetStuckLeadsCount
    @OrganizationId UNIQUEIDENTIFIER,
    @Cutoff DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM (
        SELECT l.Id FROM appuser.Leads l WITH (NOLOCK)
        JOIN appuser.PipelineStages ps WITH (NOLOCK) ON ps.Id = l.PipelineStageId
        WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1
          AND ps.IsWon = 0 AND ps.IsLost = 0
          AND COALESCE(l.PipelineStageChangedAtUtc, l.CreatedAtUtc) < @Cutoff
        UNION
        SELECT lp.LeadId FROM appuser.LeadProducts lp WITH (NOLOCK)
        JOIN appuser.ProductStages pst WITH (NOLOCK) ON pst.Id = lp.ProductStageId AND pst.OrganizationId = @OrganizationId
        WHERE lp.OrganizationId = @OrganizationId
          AND pst.IsWon = 0 AND pst.IsLost = 0
          AND COALESCE(lp.StatusChangedAtUtc, lp.CreatedAtUtc) < @Cutoff
          AND EXISTS (SELECT 1 FROM appuser.Leads l2 WITH (NOLOCK) WHERE l2.Id = lp.LeadId AND l2.IsActive = 1)
    ) AS stuck;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetLeadTrend
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL,
    @BucketByMonth BIT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        CASE WHEN @BucketByMonth = 1 THEN DATEFROMPARTS(YEAR(CreatedAtUtc), MONTH(CreatedAtUtc), 1) ELSE CAST(CreatedAtUtc AS DATE) END AS BucketStart,
        COUNT(*) AS LeadCount
    FROM appuser.Leads WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1 AND (@Since IS NULL OR CreatedAtUtc >= @Since)
      AND (@Until IS NULL OR CreatedAtUtc < @Until)
    GROUP BY CASE WHEN @BucketByMonth = 1 THEN DATEFROMPARTS(YEAR(CreatedAtUtc), MONTH(CreatedAtUtc), 1) ELSE CAST(CreatedAtUtc AS DATE) END
    ORDER BY BucketStart;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetLeadSourceBreakdown
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Source, COUNT(*) AS LeadCount
    FROM appuser.Leads WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1 AND (@Since IS NULL OR CreatedAtUtc >= @Since)
      AND (@Until IS NULL OR CreatedAtUtc < @Until)
    GROUP BY Source
    ORDER BY LeadCount DESC;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetLeadsCreatedByUser
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CreatedByUserId AS UserId, COUNT(*) AS Count
    FROM appuser.Leads WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1 AND CreatedByUserId IS NOT NULL
      AND (@Since IS NULL OR CreatedAtUtc >= @Since)
      AND (@Until IS NULL OR CreatedAtUtc < @Until)
    GROUP BY CreatedByUserId;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetTasksCompletedByUser
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AssignedToUserId AS UserId, COUNT(*) AS Count
    FROM appuser.Tasks WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND Status = 'Completed'
      AND (@Since IS NULL OR CompletedAtUtc >= @Since)
      AND (@Until IS NULL OR CompletedAtUtc < @Until)
    GROUP BY AssignedToUserId;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetTasksOverdueByUser
    @OrganizationId UNIQUEIDENTIFIER,
    @Today DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AssignedToUserId AS UserId, COUNT(*) AS Count
    FROM appuser.Tasks WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND Status = 'Open' AND DueDate < @Today
    GROUP BY AssignedToUserId;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetTaskCompletionTrend
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL,
    @BucketByMonth BIT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        CASE WHEN @BucketByMonth = 1 THEN DATEFROMPARTS(YEAR(CompletedAtUtc), MONTH(CompletedAtUtc), 1) ELSE CAST(CompletedAtUtc AS DATE) END AS BucketStart,
        COUNT(*) AS CompletedCount
    FROM appuser.Tasks WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND Status = 'Completed'
      AND (@Since IS NULL OR CompletedAtUtc >= @Since)
      AND (@Until IS NULL OR CompletedAtUtc < @Until)
    GROUP BY CASE WHEN @BucketByMonth = 1 THEN DATEFROMPARTS(YEAR(CompletedAtUtc), MONTH(CompletedAtUtc), 1) ELSE CAST(CompletedAtUtc AS DATE) END
    ORDER BY BucketStart;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetLeadsByProduct
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id AS ProductId, COALESCE(p.Name, 'No product') AS ProductName,
           COUNT(DISTINCT l.Id) AS LeadCount,
           COALESCE(SUM(lp.Quantity * lp.UnitPrice), 0) AS TotalValue
    FROM appuser.Leads l WITH (NOLOCK)
    LEFT JOIN appuser.LeadProducts lp WITH (NOLOCK) ON lp.LeadId = l.Id AND lp.OrganizationId = @OrganizationId
    LEFT JOIN appuser.Products p WITH (NOLOCK) ON p.Id = lp.ProductId AND p.OrganizationId = @OrganizationId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND (@Since IS NULL OR l.CreatedAtUtc >= @Since)
      AND (@Until IS NULL OR l.CreatedAtUtc < @Until)
    GROUP BY p.Id, p.Name
    ORDER BY LeadCount DESC;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetLeadsByCategory
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COALESCE(p.Category, 'Uncategorized') AS Category,
           COUNT(DISTINCT l.Id) AS LeadCount,
           COALESCE(SUM(lp.Quantity * lp.UnitPrice), 0) AS TotalValue
    FROM appuser.Leads l WITH (NOLOCK)
    LEFT JOIN appuser.LeadProducts lp WITH (NOLOCK) ON lp.LeadId = l.Id AND lp.OrganizationId = @OrganizationId
    LEFT JOIN appuser.Products p WITH (NOLOCK) ON p.Id = lp.ProductId AND p.OrganizationId = @OrganizationId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND (@Since IS NULL OR l.CreatedAtUtc >= @Since)
      AND (@Until IS NULL OR l.CreatedAtUtc < @Until)
    GROUP BY COALESCE(p.Category, 'Uncategorized')
    ORDER BY LeadCount DESC;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetLeadsByOwnerRaw
    @OrganizationId UNIQUEIDENTIFIER,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL(OwnerUserId, '') AS UserId, COUNT(*) AS Count
    FROM appuser.Leads WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1 AND (@Since IS NULL OR CreatedAtUtc >= @Since)
      AND (@Until IS NULL OR CreatedAtUtc < @Until)
    GROUP BY OwnerUserId;
END
GO

CREATE PROCEDURE appuser.usp_Reports_GetLeadDetailsBySource
    @OrganizationId UNIQUEIDENTIFIER,
    @Source NVARCHAR(50),
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT l.Id, l.Name, l.Email, l.Phone, l.Company, l.PipelineStageId, ps.Name AS StageName, l.OwnerUserId, l.CreatedAtUtc
    FROM appuser.Leads l WITH (NOLOCK)
    JOIN appuser.PipelineStages ps WITH (NOLOCK) ON ps.Id = l.PipelineStageId AND ps.OrganizationId = @OrganizationId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND l.Source = @Source
      AND (@Since IS NULL OR l.CreatedAtUtc >= @Since)
      AND (@Until IS NULL OR l.CreatedAtUtc < @Until)
    ORDER BY l.CreatedAtUtc DESC;
END
GO
