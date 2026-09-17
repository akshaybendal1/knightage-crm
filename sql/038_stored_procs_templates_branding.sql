CREATE PROCEDURE usp_MessageTemplates_GetAll
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Body, IsActive, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc
    FROM MessageTemplates WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId
    ORDER BY Name;
END
GO

CREATE PROCEDURE usp_MessageTemplates_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Body, IsActive, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc
    FROM MessageTemplates WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_MessageTemplates_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Body NVARCHAR(MAX),
    @IsActive BIT,
    @CreatedByUserId NVARCHAR(100) = NULL,
    @CreatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO MessageTemplates (Id, OrganizationId, Name, Body, IsActive, CreatedByUserId, CreatedAtUtc)
    VALUES (@Id, @OrganizationId, @Name, @Body, @IsActive, @CreatedByUserId, @CreatedAtUtc);
END
GO

CREATE PROCEDURE usp_MessageTemplates_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Body NVARCHAR(MAX),
    @UpdatedByUserId NVARCHAR(100) = NULL,
    @UpdatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MessageTemplates SET Name = @Name, Body = @Body, UpdatedByUserId = @UpdatedByUserId, UpdatedAtUtc = @UpdatedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_MessageTemplates_SetActive
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MessageTemplates SET IsActive = @IsActive WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_OrganizationBranding_Get
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, OrganizationId, LogoContentType, LogoData, UpdatedByUserId, UpdatedAtUtc
    FROM OrganizationBranding WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_OrganizationBranding_Upsert
    @OrganizationId UNIQUEIDENTIFIER,
    @LogoContentType NVARCHAR(100),
    @LogoData VARBINARY(MAX),
    @UpdatedByUserId NVARCHAR(100) = NULL,
    @UpdatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrganizationBranding
    SET LogoContentType = @LogoContentType, LogoData = @LogoData, UpdatedByUserId = @UpdatedByUserId, UpdatedAtUtc = @UpdatedAtUtc
    WHERE OrganizationId = @OrganizationId;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO OrganizationBranding (Id, OrganizationId, LogoContentType, LogoData, UpdatedByUserId, UpdatedAtUtc)
        VALUES (NEWID(), @OrganizationId, @LogoContentType, @LogoData, @UpdatedByUserId, @UpdatedAtUtc);
    END
END
GO

CREATE PROCEDURE usp_OrganizationBranding_Delete
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM OrganizationBranding WHERE OrganizationId = @OrganizationId;
END
GO
