-- Per-organization overrides of which screens each role may open (Access Control screen).
--
-- Only cells that DIFFER from the built-in defaults (see ScreenCatalog in Knightage.Crm.Core) are
-- stored. An organization with no rows behaves exactly as before this feature existed, and a
-- screen added to the catalog later reaches everyone who hasn't customised that cell.
--
-- Purely additive: one new table and two procedures. Nothing existing is altered or touched.
CREATE TABLE RoleScreenAccess (
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    ScreenKey NVARCHAR(50) NOT NULL,
    IsAllowed BIT NOT NULL,
    UpdatedAtUtc DATETIME2 NOT NULL,
    UpdatedByUserId NVARCHAR(100) NULL,
    CONSTRAINT PK_RoleScreenAccess PRIMARY KEY (OrganizationId, Role, ScreenKey)
);
GO

CREATE PROCEDURE usp_RoleScreenAccess_GetByOrganization
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Role, ScreenKey, IsAllowed
    FROM RoleScreenAccess WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId;
END
GO

-- Replaces the organization's whole override set in one transaction (so a failure part-way leaves
-- the previous settings intact). @Entries is "Role:ScreenKey:1,Role:ScreenKey:0,..." -- the same
-- STRING_SPLIT idiom as usp_DeviceTokens_GetTokensForUsers. Passing an empty string clears every
-- override, i.e. resets the organization to the defaults.
CREATE PROCEDURE usp_RoleScreenAccess_Replace
    @OrganizationId UNIQUEIDENTIFIER,
    @Entries NVARCHAR(MAX),
    @UpdatedByUserId NVARCHAR(100),
    @NowUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DELETE FROM RoleScreenAccess WHERE OrganizationId = @OrganizationId;

    INSERT INTO RoleScreenAccess (OrganizationId, Role, ScreenKey, IsAllowed, UpdatedAtUtc, UpdatedByUserId)
    SELECT @OrganizationId,
           SUBSTRING(s.value, 1, a.p1 - 1),
           SUBSTRING(s.value, a.p1 + 1, b.p2 - a.p1 - 1),
           CASE SUBSTRING(s.value, b.p2 + 1, 1) WHEN '1' THEN 1 ELSE 0 END,
           @NowUtc,
           @UpdatedByUserId
    FROM STRING_SPLIT(ISNULL(@Entries, ''), ',') AS s
    CROSS APPLY (SELECT CHARINDEX(':', s.value) AS p1) AS a
    CROSS APPLY (SELECT CHARINDEX(':', s.value, a.p1 + 1) AS p2) AS b
    WHERE LEN(s.value) > 0 AND a.p1 > 1 AND b.p2 > a.p1 + 1;

    COMMIT TRANSACTION;
END
GO
