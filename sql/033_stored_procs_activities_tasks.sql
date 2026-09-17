CREATE PROCEDURE usp_LeadActivities_GetByLeadId
    @LeadId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc
    FROM LeadActivities WITH (NOLOCK)
    WHERE LeadId = @LeadId AND OrganizationId = @OrganizationId
    ORDER BY CreatedAtUtc DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
GO

CREATE PROCEDURE usp_LeadActivities_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @LeadId UNIQUEIDENTIFIER,
    @Type NVARCHAR(50),
    @Content NVARCHAR(2000),
    @Metadata NVARCHAR(MAX) = NULL,
    @CreatedByUserId NVARCHAR(100) = NULL,
    @CreatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO LeadActivities (Id, OrganizationId, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc)
    VALUES (@Id, @OrganizationId, @LeadId, @Type, @Content, @Metadata, @CreatedByUserId, @CreatedAtUtc);
END
GO

CREATE PROCEDURE usp_LeadActivities_CreateMany
    @OrganizationId UNIQUEIDENTIFIER,
    @Activities LeadActivityTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO LeadActivities (Id, OrganizationId, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc)
    SELECT Id, @OrganizationId, LeadId, Type, Content, Metadata, CreatedByUserId, CreatedAtUtc
    FROM @Activities;
END
GO

CREATE PROCEDURE usp_Tasks_GetByLeadId
    @LeadId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, LeadId, Title, Description, DueDate, Status, AssignedToUserId,
           CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc, CompletedAtUtc, ReminderSentAtUtc
    FROM Tasks WITH (NOLOCK)
    WHERE LeadId = @LeadId AND OrganizationId = @OrganizationId
    ORDER BY DueDate ASC;
END
GO

CREATE PROCEDURE usp_Tasks_GetByAssigneePaged
    @OrganizationId UNIQUEIDENTIFIER,
    @AssignedToUserId NVARCHAR(100) = NULL,
    @Status NVARCHAR(20) = NULL,
    @Offset INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM Tasks t WITH (NOLOCK)
    WHERE t.OrganizationId = @OrganizationId
      AND (@AssignedToUserId IS NULL OR t.AssignedToUserId = @AssignedToUserId)
      AND (@Status IS NULL OR t.Status = @Status);

    SELECT t.Id, t.LeadId, t.Title, t.Description, t.DueDate, t.Status, t.AssignedToUserId,
           t.CreatedByUserId, t.CreatedAtUtc, t.UpdatedByUserId, t.UpdatedAtUtc, t.CompletedAtUtc, t.ReminderSentAtUtc, l.Name AS LeadName
    FROM Tasks t WITH (NOLOCK)
    JOIN Leads l WITH (NOLOCK) ON l.Id = t.LeadId
    WHERE t.OrganizationId = @OrganizationId
      AND (@AssignedToUserId IS NULL OR t.AssignedToUserId = @AssignedToUserId)
      AND (@Status IS NULL OR t.Status = @Status)
    ORDER BY t.DueDate ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE PROCEDURE usp_Tasks_GetByDateRange
    @OrganizationId UNIQUEIDENTIFIER,
    @AssignedToUserId NVARCHAR(100) = NULL,
    @From DATETIME2,
    @To DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT t.Id, t.LeadId, t.Title, t.Description, t.DueDate, t.Status, t.AssignedToUserId,
           t.CreatedByUserId, t.CreatedAtUtc, t.UpdatedByUserId, t.UpdatedAtUtc, t.CompletedAtUtc, t.ReminderSentAtUtc, l.Name AS LeadName
    FROM Tasks t WITH (NOLOCK)
    JOIN Leads l WITH (NOLOCK) ON l.Id = t.LeadId
    WHERE t.OrganizationId = @OrganizationId
      AND t.DueDate BETWEEN @From AND @To
      AND (@AssignedToUserId IS NULL OR t.AssignedToUserId = @AssignedToUserId)
    ORDER BY t.DueDate ASC;
END
GO

CREATE PROCEDURE usp_Tasks_GetDueOrOverdueCount
    @OrganizationId UNIQUEIDENTIFIER,
    @AssignedToUserId NVARCHAR(100),
    @Today DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM Tasks WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND AssignedToUserId = @AssignedToUserId AND Status = 'Open' AND DueDate <= @Today;
END
GO

CREATE PROCEDURE usp_Tasks_GetDueTomorrowUnreminded
    @OrganizationId UNIQUEIDENTIFIER,
    @Tomorrow DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT t.Id, t.LeadId, t.Title, t.Description, t.DueDate, t.Status, t.AssignedToUserId,
           t.CreatedByUserId, t.CreatedAtUtc, t.CompletedAtUtc, l.Name AS LeadName
    FROM Tasks t WITH (NOLOCK)
    JOIN Leads l WITH (NOLOCK) ON l.Id = t.LeadId
    WHERE t.OrganizationId = @OrganizationId
      AND t.Status = 'Open' AND t.DueDate <= @Tomorrow AND t.ReminderSentAtUtc IS NULL;
END
GO

-- @TaskIds is a comma-separated list of GUIDs.
CREATE PROCEDURE usp_Tasks_MarkReminded
    @OrganizationId UNIQUEIDENTIFIER,
    @TaskIds NVARCHAR(MAX),
    @SentAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Tasks SET ReminderSentAtUtc = @SentAtUtc
    WHERE OrganizationId = @OrganizationId
      AND Id IN (SELECT CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@TaskIds, ','));
END
GO

CREATE PROCEDURE usp_Tasks_GetById
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, LeadId, Title, Description, DueDate, Status, AssignedToUserId,
           CreatedByUserId, CreatedAtUtc, UpdatedByUserId, UpdatedAtUtc, CompletedAtUtc, ReminderSentAtUtc
    FROM Tasks WITH (NOLOCK)
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO

CREATE PROCEDURE usp_Tasks_Create
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @LeadId UNIQUEIDENTIFIER,
    @Title NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL,
    @DueDate DATETIME2,
    @Status NVARCHAR(20),
    @AssignedToUserId NVARCHAR(100),
    @CreatedByUserId NVARCHAR(100) = NULL,
    @CreatedAtUtc DATETIME2,
    @CompletedAtUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Tasks (Id, OrganizationId, LeadId, Title, Description, DueDate, Status, AssignedToUserId, CreatedByUserId, CreatedAtUtc, CompletedAtUtc)
    VALUES (@Id, @OrganizationId, @LeadId, @Title, @Description, @DueDate, @Status, @AssignedToUserId, @CreatedByUserId, @CreatedAtUtc, @CompletedAtUtc);
END
GO

CREATE PROCEDURE usp_Tasks_UpdateStatus
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Status NVARCHAR(20),
    @CompletedAtUtc DATETIME2 = NULL,
    @UpdatedByUserId NVARCHAR(100) = NULL,
    @UpdatedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Tasks SET Status = @Status, CompletedAtUtc = @CompletedAtUtc,
           UpdatedByUserId = @UpdatedByUserId, UpdatedAtUtc = @UpdatedAtUtc
    WHERE Id = @Id AND OrganizationId = @OrganizationId;
END
GO
