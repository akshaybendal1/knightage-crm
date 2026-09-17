-- Quotations and invoices generated from a lead's current products. A single
-- Documents table with a Type discriminator ('Quotation'/'Invoice') mirrors the
-- existing LeadActivity.Type convention, rather than two near-identical tables.
CREATE TABLE Documents (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    LeadId UNIQUEIDENTIFIER NOT NULL REFERENCES Leads(Id),
    Type NVARCHAR(20) NOT NULL,                    -- 'Quotation' | 'Invoice'
    Number NVARCHAR(20) NOT NULL,                  -- 'QUO-0001' / 'INV-0001'
    Status NVARCHAR(20) NOT NULL DEFAULT 'Draft',  -- Draft -> Sent -> (Paid, Invoice only)
    TaxRatePercent DECIMAL(5,2) NOT NULL DEFAULT 0,
    Subtotal DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    Total DECIMAL(18,2) NOT NULL,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    SentAtUtc DATETIME2 NULL,
    PaidAtUtc DATETIME2 NULL
);
CREATE INDEX IX_Documents_OrganizationId_LeadId ON Documents(OrganizationId, LeadId);
CREATE INDEX IX_Documents_OrganizationId_Type ON Documents(OrganizationId, Type);
GO

-- Snapshot of the lead's LeadProducts at generation time -- deliberately
-- duplicated data, not a live join, so a document already sent to a customer
-- never silently changes if LeadProducts are edited/removed afterward.
CREATE TABLE DocumentLineItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    DocumentId UNIQUEIDENTIFIER NOT NULL REFERENCES Documents(Id),
    ProductName NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL
);
CREATE INDEX IX_DocumentLineItems_OrganizationId_DocumentId ON DocumentLineItems(OrganizationId, DocumentId);
GO
