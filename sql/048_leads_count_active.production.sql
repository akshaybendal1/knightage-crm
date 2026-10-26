-- Backs the plan's record-limit check (LeadsController.Create, SalesEntriesController.Create)
-- and the usage-warning banner. IsActive = 1 excludes soft-deleted Leads, matching how a
-- customer would count "my current leads" -- a deleted Lead shouldn't count against their limit.
CREATE OR ALTER PROCEDURE appuser.usp_Leads_CountActive
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM appuser.Leads WITH (NOLOCK) WHERE OrganizationId = @OrganizationId AND IsActive = 1;
END
GO
