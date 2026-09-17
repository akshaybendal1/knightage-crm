-- Extends the stage stored procs from 030_stored_procs_stages.sql to carry
-- IsDefault (added in 042_stage_default_flag.sql). CREATE OR ALTER since these
-- procs already exist.

CREATE OR ALTER PROCEDURE usp_PipelineStages_GetActive
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost, IsDefault
    FROM PipelineStages WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1
    ORDER BY SortOrder;
END
GO

CREATE OR ALTER PROCEDURE usp_PipelineStages_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost, IsDefault
    FROM PipelineStages WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE OR ALTER PROCEDURE usp_PipelineStages_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsActive BIT,
    @IsWon BIT,
    @IsLost BIT,
    @IsDefault BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PipelineStages (Id, OrganizationId, Name, SortOrder, IsActive, IsWon, IsLost, IsDefault)
    VALUES (@Id, @OrganizationId, @Name, @SortOrder, @IsActive, @IsWon, @IsLost, @IsDefault);
END
GO

CREATE OR ALTER PROCEDURE usp_PipelineStages_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsWon BIT,
    @IsLost BIT,
    @IsDefault BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PipelineStages SET Name = @Name, SortOrder = @SortOrder, IsWon = @IsWon, IsLost = @IsLost, IsDefault = @IsDefault
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE OR ALTER PROCEDURE usp_ProductStages_GetActive
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost, IsDefault
    FROM ProductStages WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1
    ORDER BY SortOrder;
END
GO

CREATE OR ALTER PROCEDURE usp_ProductStages_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive, IsWon, IsLost, IsDefault
    FROM ProductStages WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE OR ALTER PROCEDURE usp_ProductStages_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsActive BIT,
    @IsWon BIT,
    @IsLost BIT,
    @IsDefault BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ProductStages (Id, OrganizationId, Name, SortOrder, IsActive, IsWon, IsLost, IsDefault)
    VALUES (@Id, @OrganizationId, @Name, @SortOrder, @IsActive, @IsWon, @IsLost, @IsDefault);
END
GO

CREATE OR ALTER PROCEDURE usp_ProductStages_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsWon BIT,
    @IsLost BIT,
    @IsDefault BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ProductStages SET Name = @Name, SortOrder = @SortOrder, IsWon = @IsWon, IsLost = @IsLost, IsDefault = @IsDefault
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO
