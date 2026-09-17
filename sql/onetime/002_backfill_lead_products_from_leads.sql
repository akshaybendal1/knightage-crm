-- Run once, manually, whenever ready -- NOT part of the numbered migration sequence
-- and NOT run automatically as part of deploying 015_product_price.sql /
-- 016_lead_products.sql. See the design note in 016_lead_products.sql: since
-- LeadProducts.UnitPrice is a permanent snapshot, running this before real prices
-- exist on Products would permanently lock every backfilled line item at UnitPrice = 0
-- (entering real prices afterward does NOT retroactively fix already-inserted rows).
-- Set real prices on your Products first (Products page), then run this.
--
-- Idempotent-ish: re-running this after some leads already have LeadProducts rows
-- (e.g. someone manually added products in the meantime) would create duplicate line
-- items for those leads' original Leads.ProductId association -- run this exactly
-- once, before anyone starts using the new product picker on affected leads.

INSERT INTO LeadProducts (Id, OrganizationId, LeadId, ProductId, Quantity, UnitPrice, CreatedAtUtc)
SELECT NEWID(), l.OrganizationId, l.Id, l.ProductId, 1, ISNULL(p.Price, 0), GETUTCDATE()
FROM Leads l
LEFT JOIN Products p ON p.Id = l.ProductId
WHERE l.ProductId IS NOT NULL;
