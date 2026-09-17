CREATE PROCEDURE appuser.usp_PipelineStages_GetActive
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost
    FROM appuser.PipelineStages WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1
    ORDER BY SortOrder;
END
GO

CREATE PROCEDURE appuser.usp_PipelineStages_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost
    FROM appuser.PipelineStages WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_PipelineStages_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsActive BIT,
    @IsWon BIT,
    @IsLost BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO appuser.PipelineStages (Id, OrganizationId, Name, SortOrder, IsActive, IsWon, IsLost)
    VALUES (@Id, @OrganizationId, @Name, @SortOrder, @IsActive, @IsWon, @IsLost);
END
GO

CREATE PROCEDURE appuser.usp_PipelineStages_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsWon BIT,
    @IsLost BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.PipelineStages SET Name = @Name, SortOrder = @SortOrder, IsWon = @IsWon, IsLost = @IsLost
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_PipelineStages_SetActive
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.PipelineStages SET IsActive = @IsActive WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_ProductStages_GetActive
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost
    FROM appuser.ProductStages WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1
    ORDER BY SortOrder;
END
GO

CREATE PROCEDURE appuser.usp_ProductStages_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost
    FROM appuser.ProductStages WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_ProductStages_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsActive BIT,
    @IsWon BIT,
    @IsLost BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO appuser.ProductStages (Id, OrganizationId, Name, SortOrder, IsActive, IsWon, IsLost)
    VALUES (@Id, @OrganizationId, @Name, @SortOrder, @IsActive, @IsWon, @IsLost);
END
GO

CREATE PROCEDURE appuser.usp_ProductStages_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsWon BIT,
    @IsLost BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.ProductStages SET Name = @Name, SortOrder = @SortOrder, IsWon = @IsWon, IsLost = @IsLost
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_ProductStages_SetActive
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.ProductStages SET IsActive = @IsActive WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO
