-- Must run after 012_products.sql -- ProductId references Products(Id).
-- Optional Product link on a Lead, plus a Lead Owner (assignable, defaults to the
-- creator at the API layer -- see LeadsController.Create). Both nullable at the DB
-- level: Product is genuinely optional, and Owner is best-effort-backfilled below
-- rather than forced NOT NULL, so ancient rows with no CreatedByUserId don't block
-- the migration.

ALTER TABLE Leads ADD ProductId UNIQUEIDENTIFIER NULL REFERENCES Products(Id);
ALTER TABLE Leads ADD OwnerUserId NVARCHAR(100) NULL;
GO

-- New batch: a column added by ALTER TABLE above isn't visible to statements
-- compiled in the same batch, so this needs its own GO.
UPDATE Leads SET OwnerUserId = CreatedByUserId WHERE OwnerUserId IS NULL AND CreatedByUserId IS NOT NULL;

CREATE INDEX IX_Leads_OrganizationId_ProductId ON Leads(OrganizationId, ProductId);
CREATE INDEX IX_Leads_OrganizationId_OwnerUserId ON Leads(OrganizationId, OwnerUserId);
