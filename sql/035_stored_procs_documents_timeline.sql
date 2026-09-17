CREATE PROCEDURE usp_Documents_GetByLeadId
    @LeadId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, OrganizationId, LeadId, Type, Number, Status, DiscountPercent, DiscountAmount, TaxRatePercent, Subtotal, TaxAmount, Total,
           CreatedByUserId, CreatedAtUtc, SentAtUtc, PaidAtUtc
    FROM Documents WITH (NOLOCK)
    WHERE LeadId = @LeadId AND OrganizationId = @OrganizationId
    ORDER BY CreatedAtUtc DESC;
END
GO

CREATE PROCEDURE usp_Documents_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, OrganizationId, LeadId, Type, Number, Status, DiscountPercent, DiscountAmount, TaxRatePercent, Subtotal, TaxAmount, Total,
           CreatedByUserId, CreatedAtUtc, SentAtUtc, PaidAtUtc
    FROM Documents WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_Documents_GetLineItems
    @DocumentId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT li.Id, li.OrganizationId, li.DocumentId, li.ProductName, li.Quantity, li.UnitPrice, li.LineTotal
    FROM DocumentLineItems li WITH (NOLOCK)
    JOIN Documents d WITH (NOLOCK) ON d.Id = li.DocumentId AND d.OrganizationId = @OrganizationId
    WHERE li.DocumentId = @DocumentId AND li.OrganizationId = @OrganizationId;
END
GO

-- Number is computed here (not by the caller) as {QUO|INV}-{sequence:D4}, sequence =
-- COUNT(*)+1 scoped by OrganizationId+Type -- no dedicated sequence table, matching this
-- codebase's simple-first convention (a small race-condition risk between the COUNT read
-- and the INSERT is already an accepted tradeoff at this app's scale, same as before this
-- moved into a stored procedure). Returns the computed Number as a one-row result set so
-- the caller can hydrate the Document.Number it just generated.
CREATE PROCEDURE usp_Documents_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @LeadId UNIQUEIDENTIFIER,
    @Type NVARCHAR(20),
    @Status NVARCHAR(20),
    @DiscountPercent DECIMAL(5,2),
    @DiscountAmount DECIMAL(18,2),
    @TaxRatePercent DECIMAL(5,2),
    @Subtotal DECIMAL(18,2),
    @TaxAmount DECIMAL(18,2),
    @Total DECIMAL(18,2),
    @CreatedByUserId NVARCHAR(100) = NULL,
    @CreatedAtUtc DATETIME2,
    @SentAtUtc DATETIME2 = NULL,
    @PaidAtUtc DATETIME2 = NULL,
    @LineItems DocumentLineItemTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Sequence INT;
    SELECT @Sequence = COUNT(*) + 1 FROM Documents WITH (NOLOCK) WHERE OrganizationId = @OrganizationId AND Type = @Type;
    DECLARE @Prefix NVARCHAR(5) = CASE WHEN @Type = 'Invoice' THEN 'INV' ELSE 'QUO' END;
    DECLARE @Number NVARCHAR(20) = @Prefix + '-' + RIGHT('0000' + CAST(@Sequence AS NVARCHAR(10)), 4);

    INSERT INTO Documents (Id, OrganizationId, LeadId, Type, Number, Status, DiscountPercent, DiscountAmount, TaxRatePercent, Subtotal, TaxAmount, Total, CreatedByUserId, CreatedAtUtc, SentAtUtc, PaidAtUtc)
    VALUES (@Id, @OrganizationId, @LeadId, @Type, @Number, @Status, @DiscountPercent, @DiscountAmount, @TaxRatePercent, @Subtotal, @TaxAmount, @Total, @CreatedByUserId, @CreatedAtUtc, @SentAtUtc, @PaidAtUtc);

    INSERT INTO DocumentLineItems (Id, OrganizationId, DocumentId, ProductName, Quantity, UnitPrice, LineTotal)
    SELECT Id, @OrganizationId, @Id, ProductName, Quantity, UnitPrice, LineTotal
    FROM @LineItems;

    SELECT @Number AS Number;
END
GO

CREATE PROCEDURE usp_Documents_UpdateStatus
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Status NVARCHAR(20),
    @SentAtUtc DATETIME2 = NULL,
    @PaidAtUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Documents SET Status = @Status,
           SentAtUtc = COALESCE(@SentAtUtc, SentAtUtc),
           PaidAtUtc = COALESCE(@PaidAtUtc, PaidAtUtc)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_Timeline_GetLeadsTimeline
    @OrganizationId UNIQUEIDENTIFIER,
    @StageId UNIQUEIDENTIFIER,
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
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND l.PipelineStageId = @StageId
      AND (@ProductId IS NULL OR EXISTS (SELECT 1 FROM LeadProducts x WITH (NOLOCK) WHERE x.LeadId = l.Id AND x.ProductId = @ProductId))
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
      AND (@Search IS NULL OR l.Name LIKE @Search);

    SELECT l.Id AS LeadId, l.Name AS LeadName, l.Company, l.Phone, l.Source, l.OwnerUserId,
           (SELECT COALESCE(SUM(Quantity * UnitPrice), 0) FROM LeadProducts WITH (NOLOCK) WHERE LeadId = l.Id AND OrganizationId = @OrganizationId) AS TotalCost,
           l.PipelineStageId, ps.Name AS StageName,
           COALESCE(l.PipelineStageChangedAtUtc, l.CreatedAtUtc) AS StageChangedAtUtc
    FROM Leads l WITH (NOLOCK)
    JOIN PipelineStages ps WITH (NOLOCK) ON ps.Id = l.PipelineStageId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND l.PipelineStageId = @StageId
      AND (@ProductId IS NULL OR EXISTS (SELECT 1 FROM LeadProducts x WITH (NOLOCK) WHERE x.LeadId = l.Id AND x.ProductId = @ProductId))
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
      AND (@Search IS NULL OR l.Name LIKE @Search)
    ORDER BY StageChangedAtUtc ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE PROCEDURE usp_Timeline_GetLeadProductsTimeline
    @OrganizationId UNIQUEIDENTIFIER,
    @StageId UNIQUEIDENTIFIER,
    @ProductId UNIQUEIDENTIFIER = NULL,
    @OwnerUserId NVARCHAR(100) = NULL,
    @Search NVARCHAR(210) = NULL,
    @Offset INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM LeadProducts lp WITH (NOLOCK)
    JOIN Leads l WITH (NOLOCK) ON l.Id = lp.LeadId AND l.OrganizationId = @OrganizationId AND l.IsActive = 1
    WHERE lp.OrganizationId = @OrganizationId AND lp.ProductStageId = @StageId
      AND (@ProductId IS NULL OR lp.ProductId = @ProductId)
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
      AND (@Search IS NULL OR l.Name LIKE @Search);

    SELECT lp.Id AS LeadProductId, lp.LeadId, l.Name AS LeadName, l.Company, l.Phone, p.Name AS ProductName,
           lp.Quantity, lp.UnitPrice, (lp.Quantity * lp.UnitPrice) AS LineTotal,
           lp.ProductStageId, pst.Name AS ProductStageName,
           COALESCE(lp.StatusChangedAtUtc, lp.CreatedAtUtc) AS StatusChangedAtUtc
    FROM LeadProducts lp WITH (NOLOCK)
    JOIN Leads l WITH (NOLOCK) ON l.Id = lp.LeadId AND l.OrganizationId = @OrganizationId AND l.IsActive = 1
    JOIN ProductStages pst WITH (NOLOCK) ON pst.Id = lp.ProductStageId AND pst.OrganizationId = @OrganizationId
    LEFT JOIN Products p WITH (NOLOCK) ON p.Id = lp.ProductId AND p.OrganizationId = @OrganizationId
    WHERE lp.OrganizationId = @OrganizationId AND lp.ProductStageId = @StageId
      AND (@ProductId IS NULL OR lp.ProductId = @ProductId)
      AND (@OwnerUserId IS NULL OR l.OwnerUserId = @OwnerUserId)
      AND (@Search IS NULL OR l.Name LIKE @Search)
    ORDER BY StatusChangedAtUtc ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
