-- FCM device tokens for Android push notifications. Keyed uniquely by Token alone,
-- not (OrganizationId, UserId, Token) -- a token identifies one physical device+app
-- install, not a user, so a different user logging into the same device must reassign
-- the existing row (see usp_DeviceTokens_Upsert's MERGE) rather than create a stale
-- duplicate that would otherwise keep receiving the previous user's reminders too.
CREATE TABLE DeviceTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    UserId NVARCHAR(100) NOT NULL,
    Token NVARCHAR(440) NOT NULL,
    Platform NVARCHAR(20) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    LastSeenAtUtc DATETIME2 NOT NULL
);
CREATE UNIQUE INDEX IX_DeviceTokens_Token ON DeviceTokens(Token);
CREATE INDEX IX_DeviceTokens_OrganizationId_UserId ON DeviceTokens(OrganizationId, UserId);
GO

CREATE PROCEDURE usp_DeviceTokens_Upsert
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @UserId NVARCHAR(100),
    @Token NVARCHAR(440),
    @Platform NVARCHAR(20),
    @NowUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    MERGE DeviceTokens AS target
    USING (SELECT @Token AS Token) AS src
    ON target.Token = src.Token
    WHEN MATCHED THEN
        UPDATE SET OrganizationId = @OrganizationId, UserId = @UserId, Platform = @Platform, LastSeenAtUtc = @NowUtc
    WHEN NOT MATCHED THEN
        INSERT (Id, OrganizationId, UserId, Token, Platform, CreatedAtUtc, LastSeenAtUtc)
        VALUES (@Id, @OrganizationId, @UserId, @Token, @Platform, @NowUtc, @NowUtc);
END
GO

-- @UserIds is a comma-separated list, same idiom as usp_Tasks_MarkReminded's @TaskIds.
CREATE PROCEDURE usp_DeviceTokens_GetTokensForUsers
    @OrganizationId UNIQUEIDENTIFIER,
    @UserIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Token
    FROM DeviceTokens WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId
      AND UserId IN (SELECT value FROM STRING_SPLIT(@UserIds, ','));
END
GO

-- Org-scoped even though Token alone is already unique, so an authenticated caller can
-- only ever remove a token belonging to their own organization -- used both by the
-- logout endpoint and by FcmPushSender cleaning up tokens FCM reports as unregistered.
CREATE PROCEDURE usp_DeviceTokens_DeleteByToken
    @OrganizationId UNIQUEIDENTIFIER,
    @Token NVARCHAR(440)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM DeviceTokens WHERE Token = @Token AND OrganizationId = @OrganizationId;
END
GO
