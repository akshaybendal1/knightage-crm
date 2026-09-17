-- Org-scoped from creation (postdates multi-tenancy, matches 012_products.sql /
-- 014_meetings.sql's pattern rather than the older ALTER-later two-step Leads/Tasks
-- went through).
--
-- UnitPrice is a snapshot of Product.Price at the moment the line item was added --
-- it does not update if the catalog price changes later, so a lead's recorded cost
-- never silently drifts. Duplicate (LeadId, ProductId) rows are allowed on purpose
-- (e.g. quoted 2 units in March, 1 more in June at a different price) -- same product
-- can appear on a lead more than once, same as a real quote/invoice.
--
-- Deliberately NOT backfilled from Leads.ProductId here -- see
-- sql/onetime/002_backfill_lead_products_from_leads.sql, run separately once real
-- prices exist on Products (Price doesn't exist until 015_product_price.sql, so an
-- immediate backfill would permanently snapshot UnitPrice = 0 for every existing lead).
--
-- Leads.ProductId itself is left untouched by this migration -- the app stops
-- reading/writing it, but the column/FK/index stay in place as an inert safety net.

CREATE TABLE LeadProducts (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    LeadId UNIQUEIDENTIFIER NOT NULL REFERENCES Leads(Id),
    ProductId UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_LeadProducts_OrganizationId_LeadId ON LeadProducts(OrganizationId, LeadId);
CREATE INDEX IX_LeadProducts_OrganizationId_ProductId ON LeadProducts(OrganizationId, ProductId);
