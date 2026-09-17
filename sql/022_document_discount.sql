-- Discount is applied to the subtotal before tax (standard invoicing order),
-- mirroring the existing TaxRatePercent/TaxAmount pair so the UI and PDF can
-- show both the entered rate and the computed amount without recomputing.
ALTER TABLE Documents ADD DiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 0;
ALTER TABLE Documents ADD DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
