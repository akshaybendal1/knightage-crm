CREATE NONCLUSTERED INDEX IX_ProductCategories_OrganizationId_IsActive
    ON ProductCategories (OrganizationId, IsActive, SortOrder);
