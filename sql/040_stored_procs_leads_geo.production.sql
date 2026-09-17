-- Extends the Lead stored procs from 032_stored_procs_leads.production.sql to carry
-- the geo columns added in 039_lead_geo_location.production.sql. CREATE OR ALTER (not
-- a bare CREATE -- these procs already exist) redefines all three in place.

CREATE OR ALTER PROCEDURE appuser.usp_Leads_GetPaged
    @OrganizationId UNIQUEIDENTIFIER,
    @PipelineStageId UNIQUEIDENTIFIER = NULL,
    @ProductId UNIQUEIDENTIFIER = NULL,
    @OwnerUserId NVARCHAR(100) = NULL,
    @Search NVARCHAR(210) = NULL,
    @Offset INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM appuser.Leads l WITH (NOLOCK)
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1
      AND (@PipelineStageId IS NULL OR l.PipelineStageId = @PipelineStageId)
      AND (@ProductId IS NULL OR EXISTS (SELECT 1 FROM appuser.LeadProducts x WITH (NOLOCK) WHERE x.LeadId = l.Id AND x.ProductId = @ProductId))
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
      AND (@Search IS NULL OR l.Name LIKE @Search OR l.Email LIKE @Search OR l.Phone LIKE @Search OR l.Company LIKE @Search);

    SELECT l.Id, l.Name, l.Email, l.Phone, l.Company, l.PipelineStageId, l.Notes, l.Source, l.OwnerUserId,
           l.CreatedByUserId, l.CreatedAtUtc, l.UpdatedByUserId, l.UpdatedAtUtc, l.PipelineStageChangedAtUtc,
           l.Latitude, l.Longitude, l.LocationCapturedAtUtc,
           COALESCE(lp.TotalCost, 0) AS TotalCost
    FROM appuser.Leads l WITH (NOLOCK)
    LEFT JOIN (
        SELECT LeadId, SUM(Quantity * UnitPrice) AS TotalCost
        FROM appuser.LeadProducts WITH (NOLOCK)
        WHERE OrganizationId = @OrganizationId
        GROUP BY LeadId
    ) lp ON lp.LeadId = l.Id
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1
      AND (@PipelineStageId IS NULL OR l.PipelineStageId = @PipelineStageId)
      AND (@ProductId IS NULL OR EXISTS (SELECT 1 FROM appuser.LeadProducts x WITH (NOLOCK) WHERE x.LeadId = l.Id AND x.ProductId = @ProductId))
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
      AND (@Search IS NULL OR l.Name LIKE @Search OR l.Email LIKE @Search OR l.Phone LIKE @Search OR l.Company LIKE @Search)
    ORDER BY l.CreatedAtUtc DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE appuser.usp_Leads_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT l.Id, l.Name, l.Email, l.Phone, l.Company, l.PipelineStageId, l.Notes, l.Source, l.OwnerUserId,
           l.CreatedByUserId, l.CreatedAtUtc, l.UpdatedByUserId, l.UpdatedAtUtc, l.PipelineStageChangedAtUtc,
           l.Latitude, l.Longitude, l.LocationCapturedAtUtc,
           (SELECT COALESCE(SUM(Quantity * UnitPrice), 0) FROM appuser.LeadProducts WITH (NOLOCK) WHERE LeadId = l.Id AND OrganizationId = @OrganizationId) AS TotalCost
    FROM appuser.Leads l WITH (NOLOCK)
    WHERE l.Id = @Id AND l.OrganizationId = @OrganizationId;
END
GO

CREATE OR ALTER PROCEDURE appuser.usp_Leads_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Email NVARCHAR(320) = NULL,
    @Phone NVARCHAR(50) = NULL,
    @Company NVARCHAR(200) = NULL,
    @PipelineStageId UNIQUEIDENTIFIER,
    @Notes NVARCHAR(1000) = NULL,
    @Source NVARCHAR(50),
    @OwnerUserId NVARCHAR(100) = NULL,
    @IsActive BIT,
    @CreatedByUserId NVARCHAR(100) = NULL,
    @CreatedAtUtc DATETIME2,
    @PipelineStageChangedAtUtc DATETIME2 = NULL,
    @Latitude DECIMAL(9,6) = NULL,
    @Longitude DECIMAL(9,6) = NULL,
    @LocationCapturedAtUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO appuser.Leads (Id, OrganizationId, Name, Email, Phone, Company, PipelineStageId, Notes, Source, OwnerUserId, IsActive, CreatedByUserId, CreatedAtUtc, PipelineStageChangedAtUtc, Latitude, Longitude, LocationCapturedAtUtc)
    VALUES (@Id, @OrganizationId, @Name, @Email, @Phone, @Company, @PipelineStageId, @Notes, @Source, @OwnerUserId, @IsActive, @CreatedByUserId, @CreatedAtUtc, @PipelineStageChangedAtUtc, @Latitude, @Longitude, @LocationCapturedAtUtc);
END
GO
