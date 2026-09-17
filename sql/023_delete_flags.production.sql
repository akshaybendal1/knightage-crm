-- Schema-qualified equivalent of 023_delete_flags.sql for production,
-- where the Leads table lives under the `appuser` schema rather than `dbo`.
ALTER TABLE appuser.Leads ADD IsActive BIT NOT NULL DEFAULT 1;
GO
