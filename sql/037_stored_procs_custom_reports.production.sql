CREATE PROCEDURE appuser.usp_CustomReports_Run
    @OrganizationId UNIQUEIDENTIFIER,
    @Dimension1 NVARCHAR(20),
    @Dimension2 NVARCHAR(20) = NULL,
    @BucketByMonth BIT,
    @Since DATETIME2 = NULL,
    @Until DATETIME2 = NULL,
    @StageIds NVARCHAR(MAX) = NULL,
    @OwnerUserIds NVARCHAR(MAX) = NULL,
    @ProductIds NVARCHAR(MAX) = NULL,
    @Sources NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Dim1Expr NVARCHAR(300) = CASE @Dimension1
        WHEN 'lead' THEN 'CAST(l.Id AS NVARCHAR(50))'
        WHEN 'stage' THEN 'CAST(l.PipelineStageId AS NVARCHAR(50))'
        WHEN 'owner' THEN 'ISNULL(l.OwnerUserId, '''')'
        WHEN 'product' THEN 'CAST(ISNULL(p.Id, ''00000000-0000-0000-0000-000000000000'') AS NVARCHAR(50))'
        WHEN 'category' THEN 'ISNULL(p.Category, '''')'
        WHEN 'source' THEN 'l.Source'
        WHEN 'createdDate' THEN CASE WHEN @BucketByMonth = 1
            THEN 'CONVERT(NVARCHAR(10), DATEFROMPARTS(YEAR(l.CreatedAtUtc), MONTH(l.CreatedAtUtc), 1), 23)'
            ELSE 'CONVERT(NVARCHAR(10), CAST(l.CreatedAtUtc AS DATE), 23)' END
        ELSE NULL
    END;
    IF @Dim1Expr IS NULL
    BEGIN
        RAISERROR('Unknown report dimension: %s', 16, 1, @Dimension1);
        RETURN;
    END

    DECLARE @Dim2Expr NVARCHAR(300) = NULL;
    IF @Dimension2 IS NOT NULL
    BEGIN
        SET @Dim2Expr = CASE @Dimension2
            WHEN 'lead' THEN 'CAST(l.Id AS NVARCHAR(50))'
            WHEN 'stage' THEN 'CAST(l.PipelineStageId AS NVARCHAR(50))'
            WHEN 'owner' THEN 'ISNULL(l.OwnerUserId, '''')'
            WHEN 'product' THEN 'CAST(ISNULL(p.Id, ''00000000-0000-0000-0000-000000000000'') AS NVARCHAR(50))'
            WHEN 'category' THEN 'ISNULL(p.Category, '''')'
            WHEN 'source' THEN 'l.Source'
            WHEN 'createdDate' THEN CASE WHEN @BucketByMonth = 1
                THEN 'CONVERT(NVARCHAR(10), DATEFROMPARTS(YEAR(l.CreatedAtUtc), MONTH(l.CreatedAtUtc), 1), 23)'
                ELSE 'CONVERT(NVARCHAR(10), CAST(l.CreatedAtUtc AS DATE), 23)' END
            ELSE NULL
        END;
        IF @Dim2Expr IS NULL
        BEGIN
            RAISERROR('Unknown report dimension: %s', 16, 1, @Dimension2);
            RETURN;
        END
    END

    DECLARE @NeedsProductJoin BIT = CASE WHEN @Dimension1 IN ('product', 'category') OR @Dimension2 IN ('product', 'category') THEN 1 ELSE 0 END;
    DECLARE @ProductJoin NVARCHAR(300) = CASE WHEN @NeedsProductJoin = 1 THEN 'LEFT JOIN appuser.Products p WITH (NOLOCK) ON p.Id = lp.ProductId AND p.OrganizationId = @OrganizationId' ELSE '' END;

    DECLARE @SelectCols NVARCHAR(700) = CASE WHEN @Dim2Expr IS NULL
        THEN @Dim1Expr + ' AS Dim1Value'
        ELSE @Dim1Expr + ' AS Dim1Value, ' + @Dim2Expr + ' AS Dim2Value' END;
    DECLARE @GroupByCols NVARCHAR(700) = CASE WHEN @Dim2Expr IS NULL THEN @Dim1Expr ELSE @Dim1Expr + ', ' + @Dim2Expr END;

    DECLARE @OrderBy NVARCHAR(50) = CASE
        WHEN @Dimension1 = 'createdDate' THEN 'Dim1Value ASC'
        WHEN @Dimension2 = 'createdDate' THEN 'Dim2Value ASC'
        ELSE 'LeadCount DESC' END;

    DECLARE @Filters NVARCHAR(MAX) = 'l.OrganizationId = @OrganizationId AND l.IsActive = 1';
    IF @Since IS NOT NULL SET @Filters += ' AND l.CreatedAtUtc >= @Since';
    IF @Until IS NOT NULL SET @Filters += ' AND l.CreatedAtUtc < @Until';
    IF @StageIds IS NOT NULL SET @Filters += ' AND l.PipelineStageId IN (SELECT CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@StageIds, '',''))';
    IF @OwnerUserIds IS NOT NULL SET @Filters += ' AND l.OwnerUserId IN (SELECT value FROM STRING_SPLIT(@OwnerUserIds, '',''))';
    IF @ProductIds IS NOT NULL SET @Filters += ' AND EXISTS (SELECT 1 FROM appuser.LeadProducts x WITH (NOLOCK) WHERE x.LeadId = l.Id AND x.ProductId IN (SELECT CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@ProductIds, '','')))';
    IF @Sources IS NOT NULL SET @Filters += ' AND l.Source IN (SELECT value FROM STRING_SPLIT(@Sources, '',''))';

    DECLARE @Sql NVARCHAR(MAX) = N'
        SELECT ' + @SelectCols + ',
               COUNT(DISTINCT l.Id) AS LeadCount,
               COALESCE(SUM(lp.Quantity * lp.UnitPrice), 0) AS TotalValue
        FROM appuser.Leads l WITH (NOLOCK)
        LEFT JOIN appuser.LeadProducts lp WITH (NOLOCK) ON lp.LeadId = l.Id AND lp.OrganizationId = @OrganizationId
        ' + @ProductJoin + '
        WHERE ' + @Filters + '
        GROUP BY ' + @GroupByCols + '
        ORDER BY ' + @OrderBy;

    EXEC sp_executesql @Sql,
        N'@OrganizationId UNIQUEIDENTIFIER, @Since DATETIME2, @Until DATETIME2, @StageIds NVARCHAR(MAX), @OwnerUserIds NVARCHAR(MAX), @ProductIds NVARCHAR(MAX), @Sources NVARCHAR(MAX)',
        @OrganizationId = @OrganizationId, @Since = @Since, @Until = @Until, @StageIds = @StageIds, @OwnerUserIds = @OwnerUserIds, @ProductIds = @ProductIds, @Sources = @Sources;
END
GO

CREATE PROCEDURE appuser.usp_SavedCustomReports_GetAll
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, DefinitionJson, IsActive, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc
    FROM appuser.SavedCustomReports WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1
    ORDER BY Name;
END
GO

CREATE PROCEDURE appuser.usp_SavedCustomReports_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, DefinitionJson, IsActive, CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc
    FROM appuser.SavedCustomReports WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_SavedCustomReports_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @DefinitionJson NVARCHAR(MAX),
    @IsActive BIT,
    @CreatedByUserId NVARCHAR(100) = NULL,
    @CreatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO appuser.SavedCustomReports (Id, OrganizationId, Name, DefinitionJson, IsActive, CreatedByUserId, CreatedAtUtc)
    VALUES (@Id, @OrganizationId, @Name, @DefinitionJson, @IsActive, @CreatedByUserId, @CreatedAtUtc);
END
GO

CREATE PROCEDURE appuser.usp_SavedCustomReports_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @DefinitionJson NVARCHAR(MAX),
    @UpdatedByUserId NVARCHAR(100) = NULL,
    @UpdatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.SavedCustomReports SET Name = @Name, DefinitionJson = @DefinitionJson, UpdatedByUserId = @UpdatedByUserId, UpdatedAtUtc = @UpdatedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_SavedCustomReports_SetActive
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.SavedCustomReports SET IsActive = @IsActive WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO
