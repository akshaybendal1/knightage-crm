-- Table types used as table-valued parameters (TVPs) for the handful of repository
-- methods that bulk-insert a variable number of full rows (CSV lead import, activity
-- batch logging, document line items). Every other list-valued filter (id lists,
-- string lists) uses STRING_SPLIT on a comma-separated parameter instead of a TVP --
-- simpler to wire up from Dapper for a plain "WHERE X IN (...)" filter, reserving TVPs
-- for the cases that actually need to carry whole rows.
CREATE TYPE LeadTvp AS TABLE (
    Id UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(320) NULL,
    Phone NVARCHAR(50) NULL,
    Company NVARCHAR(200) NULL,
    PipelineStageId UNIQUEIDENTIFIER NOT NULL,
    Notes NVARCHAR(1000) NULL,
    Source NVARCHAR(50) NOT NULL,
    OwnerUserId NVARCHAR(100) NULL,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    PipelineStageChangedAtUtc DATETIME2 NULL
);
GO

CREATE TYPE LeadActivityTvp AS TABLE (
    Id UNIQUEIDENTIFIER NOT NULL,
    LeadId UNIQUEIDENTIFIER NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    Content NVARCHAR(2000) NOT NULL,
    Metadata NVARCHAR(MAX) NULL,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL
);
GO

CREATE TYPE DocumentLineItemTvp AS TABLE (
    Id UNIQUEIDENTIFIER NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL
);
GO
