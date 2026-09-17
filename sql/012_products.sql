-- Products catalog: org-wide, managed by Owner/Admin (see ProductsController),
-- optionally linkable to a Lead. Soft-deactivate only, matching MessageTemplates'
-- IsActive-toggle pattern -- no hard delete anywhere in this codebase's CRUD screens.

CREATE TABLE Products (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    UpdatedByUserId NVARCHAR(100) NULL,
    UpdatedAtUtc DATETIME2 NULL
);

CREATE INDEX IX_Products_OrganizationId_IsActive ON Products(OrganizationId, IsActive, Name);
