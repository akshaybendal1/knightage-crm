-- Lets an admin mark exactly which Pipeline/Product Stage a Field Force Tracking
-- sales entry should land in, distinct from IsWon (a stage can be "the Won stage"
-- without being "the stage new sales entries default into", though in practice
-- they're often the same one). SalesEntriesController falls back to IsWon when no
-- stage has IsDefault set, so existing orgs keep working unchanged.
ALTER TABLE PipelineStages ADD IsDefault BIT NOT NULL DEFAULT 0;
ALTER TABLE ProductStages ADD IsDefault BIT NOT NULL DEFAULT 0;
