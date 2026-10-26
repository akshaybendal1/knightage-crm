-- PRODUCTION copy of 053_won_leads_pick.sql (objects in the appuser schema).
-- Lets an Owner/Admin choose which won leads become customers (Customers screen -> "Choose leads").
-- Purely additive: two new procedures, no table/column/procedure changed, no data touched.
-- Same "won and not yet a customer" rule as usp_Leads_GetWonWithoutCustomer (052_customers.sql).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
-- One page of won leads that are not a customer yet, newest win first, optionally filtered by
-- name / company / email / phone. TotalCount repeats on every row (0 rows = nothing matched).
CREATE PROCEDURE appuser.usp_Leads_GetWonWithoutCustomerPaged
    @OrganizationId UNIQUEIDENTIFIER,
    @Search NVARCHAR(200),
    @Offset INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Pattern NVARCHAR(210) = CASE WHEN @Search IS NULL OR LTRIM(RTRIM(@Search)) = N'' THEN NULL
        ELSE N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;

    SELECT l.Id, l.Name, l.Company, l.Email, l.Phone, l.OwnerUserId,
           s.Name AS StageName, ISNULL(l.PipelineStageChangedAtUtc, l.CreatedAtUtc) AS WonAtUtc,
           COUNT(*) OVER () AS TotalCount
    FROM appuser.Leads l WITH (NOLOCK)
    JOIN appuser.PipelineStages s WITH (NOLOCK) ON s.Id = l.PipelineStageId AND s.OrganizationId = l.OrganizationId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND s.IsWon = 1
      AND NOT EXISTS (SELECT 1 FROM appuser.CustomerLeads cl WITH (NOLOCK) WHERE cl.OrganizationId = l.OrganizationId AND cl.LeadId = l.Id)
      AND (@Pattern IS NULL OR l.Name LIKE @Pattern OR l.Company LIKE @Pattern OR l.Email LIKE @Pattern OR l.Phone LIKE @Pattern)
    ORDER BY ISNULL(l.PipelineStageChangedAtUtc, l.CreatedAtUtc) DESC, l.Id
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- The chosen leads, re-checked: only ids of this organization that are still won and still not a customer
-- come back (anything else in @Ids is silently skipped). @Ids is a comma-separated list of GUIDs --
-- STRING_SPLIT rather than a table-valued parameter, same as usp_Leads_* in 032_stored_procs_leads.sql.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE appuser.usp_Leads_GetWonWithoutCustomerByIds
    @OrganizationId UNIQUEIDENTIFIER,
    @Ids NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT l.Id, l.Name, l.Company, l.Email, l.Phone, l.OwnerUserId
    FROM appuser.Leads l WITH (NOLOCK)
    JOIN appuser.PipelineStages s WITH (NOLOCK) ON s.Id = l.PipelineStageId AND s.OrganizationId = l.OrganizationId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND s.IsWon = 1
      AND l.Id IN (SELECT TRY_CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@Ids, ','))
      AND NOT EXISTS (SELECT 1 FROM appuser.CustomerLeads cl WITH (NOLOCK) WHERE cl.OrganizationId = l.OrganizationId AND cl.LeadId = l.Id)
    ORDER BY l.CreatedAtUtc, l.Id;
END
GO
