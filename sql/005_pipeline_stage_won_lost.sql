-- Lets an org mark which pipeline stage(s) count as "won" or "lost" for dashboard
-- reporting (User Stories Slice 1, Part C / C2), instead of matching stage names.

ALTER TABLE PipelineStages ADD IsWon BIT NOT NULL DEFAULT 0;
ALTER TABLE PipelineStages ADD IsLost BIT NOT NULL DEFAULT 0;
