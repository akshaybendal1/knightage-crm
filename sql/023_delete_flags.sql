-- Soft-delete for Leads. PipelineStages and ProductStages already have IsActive
-- (001_init.sql / 021_product_stages.sql) -- only Leads is missing the column.
ALTER TABLE Leads ADD IsActive BIT NOT NULL DEFAULT 1;
GO
