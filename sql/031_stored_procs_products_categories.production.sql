CREATE PROCEDURE appuser.usp_Products_GetAll
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Description, Category, Price, IsActive, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc
    FROM appuser.Products WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId
    ORDER BY Name;
END
GO

CREATE PROCEDURE appuser.usp_Products_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Description, Category, Price, IsActive, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc
    FROM appuser.Products WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_Products_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL,
    @Category NVARCHAR(100) = NULL,
    @Price DECIMAL(18,2),
    @IsActive BIT,
    @CreatedByUserId NVARCHAR(100) = NULL,
    @CreatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO appuser.Products (Id, OrganizationId, Name, Description, Category, Price, IsActive, CreatedByUserId, CreatedAtUtc)
    VALUES (@Id, @OrganizationId, @Name, @Description, @Category, @Price, @IsActive, @CreatedByUserId, @CreatedAtUtc);
END
GO

CREATE PROCEDURE appuser.usp_Products_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL,
    @Category NVARCHAR(100) = NULL,
    @Price DECIMAL(18,2),
    @UpdatedByUserId NVARCHAR(100) = NULL,
    @UpdatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.Products SET Name = @Name, Description = @Description, Category = @Category, Price = @Price,
           UpdatedByUserId = @UpdatedByUserId, UpdatedAtUtc = @UpdatedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_Products_SetActive
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.Products SET IsActive = @IsActive WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_ProductCategories_GetActive
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive
    FROM appuser.ProductCategories WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1
    ORDER BY SortOrder;
END
GO

CREATE PROCEDURE appuser.usp_ProductCategories_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, SortOrder, IsActive
    FROM appuser.ProductCategories WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_ProductCategories_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO appuser.ProductCategories (Id, OrganizationId, Name, SortOrder, IsActive)
    VALUES (@Id, @OrganizationId, @Name, @SortOrder, @IsActive);
END
GO

CREATE PROCEDURE appuser.usp_ProductCategories_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(100),
    @SortOrder INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.ProductCategories SET Name = @Name, SortOrder = @SortOrder
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_ProductCategories_SetActive
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.ProductCategories SET IsActive = @IsActive WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO
