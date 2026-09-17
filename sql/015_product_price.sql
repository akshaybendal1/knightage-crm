-- Additive: defaults existing rows to 0 so this can never fail against
-- existing Products. Required going forward at the UI/API layer, not via a
-- DB-level constraint (a 0 price is a legitimate value, e.g. a free item).

ALTER TABLE Products ADD Price DECIMAL(18,2) NOT NULL DEFAULT 0;
