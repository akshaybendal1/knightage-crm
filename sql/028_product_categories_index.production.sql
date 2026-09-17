CREATE NONCLUSTERED INDEX IX_ProductCategories_OrganizationId_IsActive
    ON appuser.ProductCategories (OrganizationId, IsActive, SortOrder);
