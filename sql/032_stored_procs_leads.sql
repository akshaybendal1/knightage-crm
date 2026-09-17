CREATE PROCEDURE usp_Leads_GetPaged
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
    FROM Leads l WITH (NOLOCK)
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1
      AND (@PipelineStageId IS NULL OR l.PipelineStageId = @PipelineStageId)
      AND (@ProductId IS NULL OR EXISTS (SELECT 1 FROM LeadProducts x WITH (NOLOCK) WHERE x.LeadId = l.Id AND x.ProductId = @ProductId))
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
      AND (@Search IS NULL OR l.Name LIKE @Search OR l.Email LIKE @Search OR l.Phone LIKE @Search OR l.Company LIKE @Search);

    SELECT l.Id, l.Name, l.Email, l.Phone, l.Company, l.PipelineStageId, l.Notes, l.Source, l.OwnerUserId,
           l.CreatedByUserId, l.CreatedAtUtc, l.UpdatedByUserId, l.UpdatedAtUtc, l.PipelineStageChangedAtUtc,
           COALESCE(lp.TotalCost, 0) AS TotalCost
    FROM Leads l WITH (NOLOCK)
    LEFT JOIN (
        SELECT LeadId, SUM(Quantity * UnitPrice) AS TotalCost
        FROM LeadProducts WITH (NOLOCK)
        WHERE OrganizationId = @OrganizationId
        GROUP BY LeadId
    ) lp ON lp.LeadId = l.Id
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1
      AND (@PipelineStageId IS NULL OR l.PipelineStageId = @PipelineStageId)
      AND (@ProductId IS NULL OR EXISTS (SELECT 1 FROM LeadProducts x WITH (NOLOCK) WHERE x.LeadId = l.Id AND x.ProductId = @ProductId))
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
      AND (@Search IS NULL OR l.Name LIKE @Search OR l.Email LIKE @Search OR l.Phone LIKE @Search OR l.Company LIKE @Search)
    ORDER BY l.CreatedAtUtc DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE PROCEDURE usp_Leads_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT l.Id, l.Name, l.Email, l.Phone, l.Company, l.PipelineStageId, l.Notes, l.Source, l.OwnerUserId,
           l.CreatedByUserId, l.CreatedAtUtc, l.UpdatedByUserId, l.UpdatedAtUtc, l.PipelineStageChangedAtUtc,
           (SELECT COALESCE(SUM(Quantity * UnitPrice), 0) FROM LeadProducts WITH (NOLOCK) WHERE LeadId = l.Id AND OrganizationId = @OrganizationId) AS TotalCost
    FROM Leads l WITH (NOLOCK)
    WHERE l.Id = @Id AND l.OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_Leads_Create
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
    @PipelineStageChangedAtUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Leads (Id, OrganizationId, Name, Email, Phone, Company, PipelineStageId, Notes, Source, OwnerUserId, IsActive, CreatedByUserId, CreatedAtUtc, PipelineStageChangedAtUtc)
    VALUES (@Id, @OrganizationId, @Name, @Email, @Phone, @Company, @PipelineStageId, @Notes, @Source, @OwnerUserId, @IsActive, @CreatedByUserId, @CreatedAtUtc, @PipelineStageChangedAtUtc);
END
GO

CREATE PROCEDURE usp_Leads_CreateMany
    @OrganizationId UNIQUEIDENTIFIER,
    @Leads LeadTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Leads (Id, OrganizationId, Name, Email, Phone, Company, PipelineStageId, Notes, Source, OwnerUserId, IsActive, CreatedByUserId, CreatedAtUtc, PipelineStageChangedAtUtc)
    SELECT Id, @OrganizationId, Name, Email, Phone, Company, PipelineStageId, Notes, Source, OwnerUserId, 1, CreatedByUserId, CreatedAtUtc, PipelineStageChangedAtUtc
    FROM @Leads;
END
GO

CREATE PROCEDURE usp_Leads_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Email NVARCHAR(320) = NULL,
    @Phone NVARCHAR(50) = NULL,
    @Company NVARCHAR(200) = NULL,
    @PipelineStageId UNIQUEIDENTIFIER,
    @Notes NVARCHAR(1000) = NULL,
    @OwnerUserId NVARCHAR(100) = NULL,
    @UpdatedByUserId NVARCHAR(100) = NULL,
    @UpdatedAtUtc DATETIME2,
    @PipelineStageChangedAtUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Leads SET Name = @Name, Email = @Email, Phone = @Phone, Company = @Company,
           PipelineStageId = @PipelineStageId, Notes = @Notes, OwnerUserId = @OwnerUserId,
           UpdatedByUserId = @UpdatedByUserId, UpdatedAtUtc = @UpdatedAtUtc, PipelineStageChangedAtUtc = @PipelineStageChangedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_Leads_UpdateStage
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @PipelineStageId UNIQUEIDENTIFIER,
    @StageChangedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Leads SET PipelineStageId = @PipelineStageId, PipelineStageChangedAtUtc = @StageChangedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_Leads_SetActive
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Leads SET IsActive = @IsActive WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

-- @Ids is a comma-separated list of GUIDs -- STRING_SPLIT rather than a table-valued
-- parameter, since this is a plain membership filter (not carrying full rows).
CREATE PROCEDURE usp_Leads_GetNamesByIds
    @OrganizationId UNIQUEIDENTIFIER,
    @Ids NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT l.Id, l.Name
    FROM Leads l WITH (NOLOCK)
    WHERE l.OrganizationId = @OrganizationId
      AND l.Id IN (SELECT CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@Ids, ','));
END
GO
