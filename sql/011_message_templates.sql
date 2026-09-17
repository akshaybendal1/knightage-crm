-- WhatsApp message templates for the Leads screen: org-wide, managed by
-- Owner/Admin (see MessageTemplatesController), usable by any authenticated
-- user when composing a WhatsApp message to a lead. Soft-deactivate only,
-- matching Team's IsActive-toggle pattern -- no hard delete anywhere in
-- this codebase's CRUD screens.

CREATE TABLE MessageTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Body NVARCHAR(1000) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    UpdatedByUserId NVARCHAR(100) NULL,
    UpdatedAtUtc DATETIME2 NULL
);

CREATE INDEX IX_MessageTemplates_OrganizationId_IsActive ON MessageTemplates(OrganizationId, IsActive, Name);
