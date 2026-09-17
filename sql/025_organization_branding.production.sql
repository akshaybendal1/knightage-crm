CREATE TABLE appuser.OrganizationBranding (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    LogoContentType NVARCHAR(100) NOT NULL,
    LogoData VARBINARY(MAX) NOT NULL,
    UpdatedByUserId NVARCHAR(100) NULL,
    UpdatedAtUtc DATETIME2 NOT NULL
);

CREATE UNIQUE INDEX IX_OrganizationBranding_OrganizationId ON appuser.OrganizationBranding(OrganizationId);
