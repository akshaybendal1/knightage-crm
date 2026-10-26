CREATE TABLE appuser.DeviceTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    UserId NVARCHAR(100) NOT NULL,
    Token NVARCHAR(440) NOT NULL,
    Platform NVARCHAR(20) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    LastSeenAtUtc DATETIME2 NOT NULL
);
CREATE UNIQUE INDEX IX_DeviceTokens_Token ON appuser.DeviceTokens(Token);
CREATE INDEX IX_DeviceTokens_OrganizationId_UserId ON appuser.DeviceTokens(OrganizationId, UserId);
GO

CREATE PROCEDURE appuser.usp_DeviceTokens_Upsert
    @Id UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @UserId NVARCHAR(100),
    @Token NVARCHAR(440),
    @Platform NVARCHAR(20),
    @NowUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    MERGE appuser.DeviceTokens AS target
    USING (SELECT @Token AS Token) AS src
    ON target.Token = src.Token
    WHEN MATCHED THEN
        UPDATE SET OrganizationId = @OrganizationId, UserId = @UserId, Platform = @Platform, LastSeenAtUtc = @NowUtc
    WHEN NOT MATCHED THEN
        INSERT (Id, OrganizationId, UserId, Token, Platform, CreatedAtUtc, LastSeenAtUtc)
        VALUES (@Id, @OrganizationId, @UserId, @Token, @Platform, @NowUtc, @NowUtc);
END
GO

CREATE PROCEDURE appuser.usp_DeviceTokens_GetTokensForUsers
    @OrganizationId UNIQUEIDENTIFIER,
    @UserIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Token
    FROM appuser.DeviceTokens WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId
      AND UserId IN (SELECT value FROM STRING_SPLIT(@UserIds, ','));
END
GO

CREATE PROCEDURE appuser.usp_DeviceTokens_DeleteByToken
    @OrganizationId UNIQUEIDENTIFIER,
    @Token NVARCHAR(440)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM appuser.DeviceTokens WHERE Token = @Token AND OrganizationId = @OrganizationId;
END
GO
