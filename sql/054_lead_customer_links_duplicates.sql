-- Lead <-> customer links and duplicate detection for manually entered leads. Purely additive: five new procedures. No table,
-- column or existing procedure is changed and no data is touched.
--
--   usp_Leads_FindDuplicates      active leads that look like the one being typed (same email, same phone, or same name + company)
--   usp_Customers_FindDuplicates  customers that look like it (same rules, phone compared through the stored PhoneKey)
--   usp_Customers_Search          type-ahead search of customers by name / organization / email / phone
--   usp_Customers_GetForLead      the customer a lead is linked to (CustomerLeads), if any
--   usp_CustomerLeads_Unlink      removes only the link row; the lead and the customer both stay
--
-- Phone numbers are compared by their last 10 digits (PhoneKey), like usp_Customers_Upsert. Leads store the number as typed, so
-- the same normalisation is done on the fly for them.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE PROCEDURE usp_Leads_FindDuplicates
    @OrganizationId UNIQUEIDENTIFIER,
    @Email NVARCHAR(320),
    @PhoneKey NVARCHAR(20),
    @Name NVARCHAR(200),
    @Company NVARCHAR(200),
    @ExcludeLeadId UNIQUEIDENTIFIER,
    @Max INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Max) l.Id, l.Name, l.Company, l.Email, l.Phone, s.Name AS StageName, l.OwnerUserId, l.CreatedAtUtc,
           CASE WHEN @Email IS NOT NULL AND LOWER(l.Email) = LOWER(@Email) THEN N'email'
                WHEN @PhoneKey IS NOT NULL AND RIGHT(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(l.Phone, N' ', N''), N'-', N''), N'(', N''), N')', N''), N'+', N''), N'.', N''), 10) = @PhoneKey THEN N'phone'
                ELSE N'name+company' END AS MatchedOn
    FROM Leads l WITH (NOLOCK)
    LEFT JOIN PipelineStages s WITH (NOLOCK) ON s.Id = l.PipelineStageId AND s.OrganizationId = l.OrganizationId
    WHERE l.OrganizationId = @OrganizationId AND l.IsActive = 1
      AND (@ExcludeLeadId IS NULL OR l.Id <> @ExcludeLeadId)
      AND (   (@Email IS NOT NULL AND LOWER(l.Email) = LOWER(@Email))
           OR (@PhoneKey IS NOT NULL AND RIGHT(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(l.Phone, N' ', N''), N'-', N''), N'(', N''), N')', N''), N'+', N''), N'.', N''), 10) = @PhoneKey)
           OR (@Name IS NOT NULL AND LOWER(l.Name) = LOWER(@Name) AND ISNULL(LOWER(l.Company), N'') = ISNULL(LOWER(@Company), N'')
               AND (@Company IS NOT NULL OR (@Email IS NULL AND @PhoneKey IS NULL))))
    ORDER BY CASE WHEN @Email IS NOT NULL AND LOWER(l.Email) = LOWER(@Email) THEN 1
                  WHEN @PhoneKey IS NOT NULL AND RIGHT(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(l.Phone, N' ', N''), N'-', N''), N'(', N''), N')', N''), N'+', N''), N'.', N''), 10) = @PhoneKey THEN 2
                  ELSE 3 END, l.CreatedAtUtc DESC;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_FindDuplicates
    @OrganizationId UNIQUEIDENTIFIER,
    @Email NVARCHAR(320),
    @PhoneKey NVARCHAR(20),
    @Name NVARCHAR(200),
    @Company NVARCHAR(200),
    @Max INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Max) c.Id, c.Name, c.Company, c.Email, c.Phone,
           CASE WHEN @Email IS NOT NULL AND LOWER(c.Email) = LOWER(@Email) THEN N'email'
                WHEN @PhoneKey IS NOT NULL AND c.PhoneKey = @PhoneKey THEN N'phone'
                ELSE N'name+company' END AS MatchedOn
    FROM Customers c WITH (NOLOCK)
    WHERE c.OrganizationId = @OrganizationId AND c.IsActive = 1
      AND (   (@Email IS NOT NULL AND LOWER(c.Email) = LOWER(@Email))
           OR (@PhoneKey IS NOT NULL AND c.PhoneKey = @PhoneKey)
           OR (@Name IS NOT NULL AND LOWER(c.Name) = LOWER(@Name) AND ISNULL(LOWER(c.Company), N'') = ISNULL(LOWER(@Company), N'')
               AND (@Company IS NOT NULL OR (@Email IS NULL AND @PhoneKey IS NULL))))
    ORDER BY CASE WHEN @Email IS NOT NULL AND LOWER(c.Email) = LOWER(@Email) THEN 1
                  WHEN @PhoneKey IS NOT NULL AND c.PhoneKey = @PhoneKey THEN 2
                  ELSE 3 END, c.CreatedAtUtc;
END
GO

-- @Pattern is already a LIKE pattern built by the application with \ as the escape character (so a typed % or _ is literal).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_Search
    @OrganizationId UNIQUEIDENTIFIER,
    @Pattern NVARCHAR(420),
    @Prefix NVARCHAR(420),
    @Max INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Max) c.Id, c.OrganizationId, c.Name, c.Company, c.Email, c.Phone, c.Source, c.IsActive,
           c.InventoryCustomerId, c.InventorySyncedAtUtc, c.CreatedAtUtc, 0 AS LeadCount
    FROM Customers c WITH (NOLOCK)
    WHERE c.OrganizationId = @OrganizationId AND c.IsActive = 1
      AND (c.Name LIKE @Pattern ESCAPE N'\' OR c.Company LIKE @Pattern ESCAPE N'\' OR c.Email LIKE @Pattern ESCAPE N'\' OR c.Phone LIKE @Pattern ESCAPE N'\')
    ORDER BY CASE WHEN c.Name LIKE @Prefix ESCAPE N'\' THEN 0 ELSE 1 END, c.Name;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_Customers_GetForLead
    @OrganizationId UNIQUEIDENTIFIER,
    @LeadId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id, c.OrganizationId, c.Name, c.Company, c.Email, c.Phone, c.Source, c.IsActive,
           c.InventoryCustomerId, c.InventorySyncedAtUtc, c.CreatedAtUtc,
           (SELECT COUNT(*) FROM CustomerLeads x WITH (NOLOCK) WHERE x.OrganizationId = c.OrganizationId AND x.CustomerId = c.Id) AS LeadCount
    FROM CustomerLeads cl WITH (NOLOCK)
    JOIN Customers c WITH (NOLOCK) ON c.Id = cl.CustomerId AND c.OrganizationId = cl.OrganizationId
    WHERE cl.OrganizationId = @OrganizationId AND cl.LeadId = @LeadId;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE PROCEDURE usp_CustomerLeads_Unlink
    @OrganizationId UNIQUEIDENTIFIER,
    @LeadId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM CustomerLeads WHERE OrganizationId = @OrganizationId AND LeadId = @LeadId;
END
GO
