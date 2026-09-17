CREATE PROCEDURE appuser.usp_Meetings_GetByLeadId
    @LeadId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, LeadId, Title, Description, StartAtUtc, EndAtUtc, Location, Status, OwnerUserId,
           GoogleEventId, GoogleSyncStatus, GoogleSyncErrorAtUtc,
           CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc, CancelledAtUtc, ReminderSentAtUtc
    FROM appuser.Meetings WITH (NOLOCK)
    WHERE LeadId = @LeadId AND OrganizationId = @OrganizationId
    ORDER BY StartAtUtc ASC;
END
GO

CREATE PROCEDURE appuser.usp_Meetings_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, LeadId, Title, Description, StartAtUtc, EndAtUtc, Location, Status, OwnerUserId,
           GoogleEventId, GoogleSyncStatus, GoogleSyncErrorAtUtc,
           CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc, CancelledAtUtc, ReminderSentAtUtc
    FROM appuser.Meetings WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_Meetings_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @LeadId UNIQUEIDENTIFIER,
    @Title NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL,
    @StartAtUtc DATETIME2,
    @EndAtUtc DATETIME2,
    @Location NVARCHAR(200) = NULL,
    @Status NVARCHAR(20),
    @OwnerUserId NVARCHAR(100),
    @GoogleEventId NVARCHAR(200) = NULL,
    @GoogleSyncStatus NVARCHAR(20),
    @CreatedByUserId NVARCHAR(100) = NULL,
    @CreatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO appuser.Meetings (Id, OrganizationId, LeadId, Title, Description, StartAtUtc, EndAtUtc, Location, Status, OwnerUserId,
                                   GoogleEventId, GoogleSyncStatus, CreatedByUserId, CreatedAtUtc)
    VALUES (@Id, @OrganizationId, @LeadId, @Title, @Description, @StartAtUtc, @EndAtUtc, @Location, @Status, @OwnerUserId,
            @GoogleEventId, @GoogleSyncStatus, @CreatedByUserId, @CreatedAtUtc);
END
GO

CREATE PROCEDURE appuser.usp_Meetings_Update
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Title NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL,
    @StartAtUtc DATETIME2,
    @EndAtUtc DATETIME2,
    @Location NVARCHAR(200) = NULL,
    @UpdatedByUserId NVARCHAR(100) = NULL,
    @UpdatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.Meetings SET Title = @Title, Description = @Description, StartAtUtc = @StartAtUtc, EndAtUtc = @EndAtUtc,
           Location = @Location, UpdatedByUserId = @UpdatedByUserId, UpdatedAtUtc = @UpdatedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_Meetings_UpdateStatus
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Status NVARCHAR(20),
    @CancelledAtUtc DATETIME2 = NULL,
    @UpdatedByUserId NVARCHAR(100) = NULL,
    @UpdatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.Meetings SET Status = @Status, CancelledAtUtc = @CancelledAtUtc,
           UpdatedByUserId = @UpdatedByUserId, UpdatedAtUtc = @UpdatedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_Meetings_UpdateGoogleSync
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @GoogleEventId NVARCHAR(200) = NULL,
    @GoogleSyncStatus NVARCHAR(20),
    @ErrorAtUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.Meetings SET GoogleEventId = @GoogleEventId, GoogleSyncStatus = @GoogleSyncStatus, GoogleSyncErrorAtUtc = @ErrorAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_Meetings_GetStartingSoonUnreminded
    @OrganizationId UNIQUEIDENTIFIER,
    @NowUtc DATETIME2,
    @UntilUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Id, m.LeadId, m.Title, m.Description, m.StartAtUtc, m.EndAtUtc, m.Location, m.Status, m.OwnerUserId,
           m.GoogleEventId, m.GoogleSyncStatus, m.GoogleSyncErrorAtUtc,
           m.CreatedByUserId, m.CreatedAtUtc, m.UpdatedByUserId, m.UpdatedAtUtc, m.CancelledAtUtc, l.Name AS LeadName
    FROM appuser.Meetings m WITH (NOLOCK)
    JOIN appuser.Leads l WITH (NOLOCK) ON l.Id = m.LeadId
    WHERE m.OrganizationId = @OrganizationId
      AND m.Status = 'Scheduled' AND m.StartAtUtc <= @UntilUtc AND m.EndAtUtc >= @NowUtc
      AND m.ReminderSentAtUtc IS NULL;
END
GO

CREATE PROCEDURE appuser.usp_Meetings_MarkReminded
    @OrganizationId UNIQUEIDENTIFIER,
    @MeetingIds NVARCHAR(MAX),
    @SentAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.Meetings SET ReminderSentAtUtc = @SentAtUtc
    WHERE OrganizationId = @OrganizationId
      AND Id IN (SELECT CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@MeetingIds, ','));
END
GO

CREATE PROCEDURE appuser.usp_Meetings_GetUpcomingForOwner
    @OrganizationId UNIQUEIDENTIFIER,
    @OwnerUserId NVARCHAR(100),
    @NowUtc DATETIME2,
    @UntilUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Id, m.LeadId, m.Title, m.Description, m.StartAtUtc, m.EndAtUtc, m.Location, m.Status, m.OwnerUserId,
           m.GoogleEventId, m.GoogleSyncStatus, m.GoogleSyncErrorAtUtc,
           m.CreatedByUserId, m.CreatedAtUtc, m.UpdatedByUserId, m.UpdatedAtUtc, m.CancelledAtUtc, m.ReminderSentAtUtc, l.Name AS LeadName
    FROM appuser.Meetings m WITH (NOLOCK)
    JOIN appuser.Leads l WITH (NOLOCK) ON l.Id = m.LeadId
    WHERE m.OrganizationId = @OrganizationId AND m.OwnerUserId = @OwnerUserId
      AND m.Status = 'Scheduled' AND m.StartAtUtc <= @UntilUtc AND m.EndAtUtc >= @NowUtc
    ORDER BY m.StartAtUtc ASC;
END
GO

CREATE PROCEDURE appuser.usp_LeadProducts_GetByLeadId
    @LeadId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT lp.Id, lp.OrganizationId, lp.LeadId, lp.ProductId, lp.Quantity, lp.UnitPrice, lp.CreatedAtUtc,
           lp.ProductStageId, lp.StatusChangedAtUtc, p.Name AS ProductName, ps.Name AS ProductStageName
    FROM appuser.LeadProducts lp WITH (NOLOCK)
    LEFT JOIN appuser.Products p WITH (NOLOCK) ON p.Id = lp.ProductId AND p.OrganizationId = @OrganizationId
    LEFT JOIN appuser.ProductStages ps WITH (NOLOCK) ON ps.Id = lp.ProductStageId AND ps.OrganizationId = @OrganizationId
    WHERE lp.LeadId = @LeadId AND lp.OrganizationId = @OrganizationId
    ORDER BY lp.CreatedAtUtc ASC;
END
GO

CREATE PROCEDURE appuser.usp_LeadProducts_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, OrganizationId, LeadId, ProductId, Quantity, UnitPrice, CreatedAtUtc, ProductStageId, StatusChangedAtUtc
    FROM appuser.LeadProducts WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_LeadProducts_Add
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @LeadId UNIQUEIDENTIFIER,
    @ProductId UNIQUEIDENTIFIER,
    @Quantity INT,
    @UnitPrice DECIMAL(18,2),
    @CreatedAtUtc DATETIME2,
    @ProductStageId UNIQUEIDENTIFIER,
    @StatusChangedAtUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO appuser.LeadProducts (Id, OrganizationId, LeadId, ProductId, Quantity, UnitPrice, CreatedAtUtc, ProductStageId, StatusChangedAtUtc)
    VALUES (@Id, @OrganizationId, @LeadId, @ProductId, @Quantity, @UnitPrice, @CreatedAtUtc, @ProductStageId, @StatusChangedAtUtc);
END
GO

CREATE PROCEDURE appuser.usp_LeadProducts_UpdateQuantity
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.LeadProducts SET Quantity = @Quantity WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_LeadProducts_UpdateProductStage
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @ProductStageId UNIQUEIDENTIFIER,
    @StatusChangedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE appuser.LeadProducts SET ProductStageId = @ProductStageId, StatusChangedAtUtc = @StatusChangedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE appuser.usp_LeadProducts_Remove
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM appuser.LeadProducts WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO
