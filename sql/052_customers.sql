-- Customers: a lead that is won becomes a customer (and shows up in the field sales form and in the
-- Inventory portal). Purely additive -- two new tables and a handful of procedures. No existing table,
-- column or procedure is altered, and nothing is converted automatically when this script runs: leads that
-- were already won before this script become customers only when an Owner/Admin chooses "Import from won
-- leads" on the Customers screen.
--
-- One customer can come from several leads (a repeat buyer), so the link lives in CustomerLeads.
-- PhoneKey is the last 10 digits of the phone number (NULL when there are fewer than 7 digits) so
-- "+91 98765-43210" and "9876543210" match; the application computes it.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE TABLE Customers (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Company NVARCHAR(200) NULL,
    Email NVARCHAR(320) NULL,
    Phone NVARCHAR(50) NULL,
    PhoneKey NVARCHAR(20) NULL,
    Source NVARCHAR(30) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT 1,
    InventoryCustomerId UNIQUEIDENTIFIER NULL,
    InventorySyncedAtUtc DATETIME2 NULL,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL
);
CREATE INDEX IX_Customers_OrganizationId_Name ON Customers(OrganizationId, Name);
CREATE INDEX IX_Customers_OrganizationId_Email ON Customers(OrganizationId, Email);
CREATE INDEX IX_Customers_OrganizationId_PhoneKey ON Customers(OrganizationId, PhoneKey);

CREATE TABLE CustomerLeads (
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    LeadId UNIQUEIDENTIFIER NOT NULL,
    CustomerId UNIQUEIDENTIFIER NOT NULL,
    LinkedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT PK_CustomerLeads PRIMARY KEY (OrganizationId, LeadId)
);
CREATE INDEX IX_CustomerLeads_CustomerId ON CustomerLeads(CustomerId);
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_GetAll
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id, c.OrganizationId, c.Name, c.Company, c.Email, c.Phone, c.Source, c.IsActive,
           c.InventoryCustomerId, c.InventorySyncedAtUtc, c.CreatedAtUtc,
           (SELECT COUNT(*) FROM CustomerLeads cl WITH (NOLOCK) WHERE cl.OrganizationId = c.OrganizationId AND cl.CustomerId = c.Id) AS LeadCount
    FROM Customers c WITH (NOLOCK)
    WHERE c.OrganizationId = @OrganizationId
    ORDER BY c.Name, c.CreatedAtUtc;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_GetById
    @OrganizationId UNIQUEIDENTIFIER,
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id, c.OrganizationId, c.Name, c.Company, c.Email, c.Phone, c.Source, c.IsActive,
           c.InventoryCustomerId, c.InventorySyncedAtUtc, c.CreatedAtUtc, 0 AS LeadCount
    FROM Customers c WITH (NOLOCK)
    WHERE c.OrganizationId = @OrganizationId AND c.Id = @Id;
END
GO

-- Finds the customer this person already is, or creates one. Matching, in order: the lead was already
-- converted; same email; same phone; same name + company (only when the incoming record has neither email
-- nor phone, so two people who merely share a first name stay apart). The check and the insert run under a
-- per-organization lock inside one transaction, so two simultaneous conversions of the same person can
-- never produce two customers. An existing customer is returned unchanged -- nothing is ever overwritten.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_Upsert
    @OrganizationId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Company NVARCHAR(200),
    @Email NVARCHAR(320),
    @Phone NVARCHAR(50),
    @PhoneKey NVARCHAR(20),
    @Source NVARCHAR(30),
    @LeadId UNIQUEIDENTIFIER,
    @CreatedByUserId NVARCHAR(100),
    @NowUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @lock INT;
    DECLARE @resource NVARCHAR(100) = N'customers:' + CAST(@OrganizationId AS NVARCHAR(36));
    EXEC @lock = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @lock < 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50010, 'Could not lock the customer list to add a customer. Try again.', 1;
    END

    DECLARE @CustomerId UNIQUEIDENTIFIER = NULL;
    DECLARE @WasCreated BIT = 0;

    IF @LeadId IS NOT NULL
        SELECT @CustomerId = CustomerId FROM CustomerLeads WHERE OrganizationId = @OrganizationId AND LeadId = @LeadId;

    IF @CustomerId IS NULL AND @Email IS NOT NULL
        SELECT TOP (1) @CustomerId = Id FROM Customers
        WHERE OrganizationId = @OrganizationId AND LOWER(Email) = LOWER(@Email)
        ORDER BY CreatedAtUtc;

    IF @CustomerId IS NULL AND @PhoneKey IS NOT NULL
        SELECT TOP (1) @CustomerId = Id FROM Customers
        WHERE OrganizationId = @OrganizationId AND PhoneKey = @PhoneKey
        ORDER BY CreatedAtUtc;

    IF @CustomerId IS NULL AND @Email IS NULL AND @PhoneKey IS NULL
        SELECT TOP (1) @CustomerId = Id FROM Customers
        WHERE OrganizationId = @OrganizationId
          AND LOWER(Name) = LOWER(@Name)
          AND ISNULL(LOWER(Company), N'') = ISNULL(LOWER(@Company), N'')
        ORDER BY CreatedAtUtc;

    IF @CustomerId IS NULL
    BEGIN
        SET @CustomerId = NEWID();
        INSERT INTO Customers (Id, OrganizationId, Name, Company, Email, Phone, PhoneKey, Source, IsActive, CreatedByUserId, CreatedAtUtc)
        VALUES (@CustomerId, @OrganizationId, @Name, @Company, @Email, @Phone, @PhoneKey, @Source, 1, @CreatedByUserId, @NowUtc);
        SET @WasCreated = 1;
    END

    IF @LeadId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM CustomerLeads WHERE OrganizationId = @OrganizationId AND LeadId = @LeadId)
        INSERT INTO CustomerLeads (OrganizationId, LeadId, CustomerId, LinkedAtUtc)
        VALUES (@OrganizationId, @LeadId, @CustomerId, @NowUtc);

    COMMIT TRANSACTION;

    SELECT c.Id, c.OrganizationId, c.Name, c.Company, c.Email, c.Phone, c.Source, c.IsActive,
           c.InventoryCustomerId, c.InventorySyncedAtUtc, c.CreatedAtUtc, 0 AS LeadCount, @WasCreated AS WasCreated
    FROM Customers c
    WHERE c.Id = @CustomerId;
END
GO

-- Records that a customer was chosen for a lead (a field sale for an existing customer).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_CustomerLeads_Link
    @OrganizationId UNIQUEIDENTIFIER,
    @LeadId UNIQUEIDENTIFIER,
    @CustomerId UNIQUEIDENTIFIER,
    @NowUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM CustomerLeads WHERE OrganizationId = @OrganizationId AND LeadId = @LeadId)
        INSERT INTO CustomerLeads (OrganizationId, LeadId, CustomerId, LinkedAtUtc)
        VALUES (@OrganizationId, @LeadId, @CustomerId, @NowUtc);
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_SetInventoryLink
    @OrganizationId UNIQUEIDENTIFIER,
    @Id UNIQUEIDENTIFIER,
    @InventoryCustomerId UNIQUEIDENTIFIER,
    @NowUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Customers
    SET InventoryCustomerId = @InventoryCustomerId, InventorySyncedAtUtc = @NowUtc
    WHERE OrganizationId = @OrganizationId AND Id = @Id;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_GetPendingInventory
    @OrganizationId UNIQUEIDENTIFIER,
    @Max INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Max) Id, OrganizationId, Name, Company, Email, Phone, Source, IsActive,
           InventoryCustomerId, InventorySyncedAtUtc, CreatedAtUtc, 0 AS LeadCount
    FROM Customers WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1 AND InventoryCustomerId IS NULL
    ORDER BY CreatedAtUtc;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_CountPendingInventory
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM Customers WITH (NOLOCK)
    WHERE OrganizationId = @OrganizationId AND IsActive = 1 AND InventoryCustomerId IS NULL;
END
GO

-- Active leads sitting in a Won stage that have not been linked to a customer yet.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Leads_GetWonWithoutCustomer
    @OrganizationId UNIQUEIDENTIFIER,
    @Max INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Max) l.Id, l.Name, l.Company, l.Email, l.Phone, l.OwnerUserId
    FROM Leads l WITH (NOLOCK)
    JOIN PipelineStages s WITH (NOLOCK) ON s.Id = l.PipelineStageId AND s.OrganizationId = l.OrganizationId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND s.IsWon = 1
      AND NOT EXISTS (SELECT 1 FROM CustomerLeads cl WITH (NOLOCK) WHERE cl.OrganizationId = l.OrganizationId AND cl.LeadId = l.Id)
    ORDER BY l.CreatedAtUtc, l.Id;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Leads_CountWonWithoutCustomer
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*)
    FROM Leads l WITH (NOLOCK)
    JOIN PipelineStages s WITH (NOLOCK) ON s.Id = l.PipelineStageId AND s.OrganizationId = l.OrganizationId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1 AND s.IsWon = 1
      AND NOT EXISTS (SELECT 1 FROM CustomerLeads cl WITH (NOLOCK) WHERE cl.OrganizationId = l.OrganizationId AND cl.LeadId = l.Id);
END
GO
