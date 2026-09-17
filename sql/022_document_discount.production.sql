-- Schema-qualified equivalent of 022_document_discount.sql for production,
-- where the Documents table lives under the `appuser` schema rather than `dbo`.
ALTER TABLE appuser.Documents ADD DiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 0;
ALTER TABLE appuser.Documents ADD DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
