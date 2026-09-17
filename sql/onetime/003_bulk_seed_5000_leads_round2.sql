-- One-time production data seed: 5000 additional leads for org 578BCEEF-8DC7-4D8E-A1E4-39425A663841,
-- spread across the last 3 months, each with a realistic chance of an attached product line item
-- and a follow-up task. Second round of bulk seeding for this org (first round used Source='BulkSeed';
-- this one uses 'BulkSeed2' so the two batches stay independently identifiable/reversible).
--
-- IMPORTANT -- NEWID() placement: every "random" pick below is computed as a plain inline scalar
-- column inside a SELECT ... INTO that materializes immediately into a real temp table. NEWID() is
-- NEVER used inside a JOIN's ON clause or an uncorrelated WHERE -- SQL Server's optimizer can hoist
-- and cache such an expression, evaluating it ONCE for the whole query instead of once per row,
-- collapsing every row's "random" pick to the same single value. This was found and fixed the hard
-- way earlier in this project (see 3000-lead round's own script/notes) -- do not reintroduce it.
--
-- To remove this batch later (run against production via appuser_ab, so schema-qualified):
--   DELETE FROM appuser.Tasks WHERE LeadId IN (SELECT Id FROM appuser.Leads WHERE OrganizationId = '578BCEEF-8DC7-4D8E-A1E4-39425A663841' AND Source = 'BulkSeed2');
--   DELETE FROM appuser.LeadProducts WHERE OrganizationId = '578BCEEF-8DC7-4D8E-A1E4-39425A663841' AND LeadId IN (SELECT Id FROM appuser.Leads WHERE OrganizationId = '578BCEEF-8DC7-4D8E-A1E4-39425A663841' AND Source = 'BulkSeed2');
--   DELETE FROM appuser.Leads WHERE OrganizationId = '578BCEEF-8DC7-4D8E-A1E4-39425A663841' AND Source = 'BulkSeed2';

SET NOCOUNT ON;

DECLARE @OrgId UNIQUEIDENTIFIER = '578BCEEF-8DC7-4D8E-A1E4-39425A663841';
DECLARE @UserA NVARCHAR(100) = 'BB7F4F48-5EBE-46DE-A78C-8350A6014D55';
DECLARE @UserB NVARCHAR(100) = '162C4AC6-BB30-42A4-8A1E-5F807F7964DE';
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Pipeline stage weighting: 20 slots -> New 7 (35%), Contacted 5 (25%), Qualified 3 (15%),
-- Proposal Sent 2 (10%), Won 2 (10%), Lost 1 (5%) -- a plausible funnel shape, not a uniform pick.
DECLARE @StageMap TABLE (SlotIdx INT PRIMARY KEY, PipelineStageId UNIQUEIDENTIFIER);
INSERT INTO @StageMap (SlotIdx, PipelineStageId)
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, StageId
FROM (VALUES
    ('9B6C2A9C-927A-4051-ADC3-A6A61ADED905'), ('9B6C2A9C-927A-4051-ADC3-A6A61ADED905'), ('9B6C2A9C-927A-4051-ADC3-A6A61ADED905'),
    ('9B6C2A9C-927A-4051-ADC3-A6A61ADED905'), ('9B6C2A9C-927A-4051-ADC3-A6A61ADED905'), ('9B6C2A9C-927A-4051-ADC3-A6A61ADED905'),
    ('9B6C2A9C-927A-4051-ADC3-A6A61ADED905'),
    ('AA03E71C-615B-47BE-A08B-BFAB8A6D3673'), ('AA03E71C-615B-47BE-A08B-BFAB8A6D3673'), ('AA03E71C-615B-47BE-A08B-BFAB8A6D3673'),
    ('AA03E71C-615B-47BE-A08B-BFAB8A6D3673'), ('AA03E71C-615B-47BE-A08B-BFAB8A6D3673'),
    ('C0C67FD4-1495-4B66-9C99-CB7B06DC98BA'), ('C0C67FD4-1495-4B66-9C99-CB7B06DC98BA'), ('C0C67FD4-1495-4B66-9C99-CB7B06DC98BA'),
    ('FD4F688C-6657-44E1-81F1-B879E4C47EFB'), ('FD4F688C-6657-44E1-81F1-B879E4C47EFB'),
    ('5EC2214A-5AAD-4647-B17B-59534252A678'), ('5EC2214A-5AAD-4647-B17B-59534252A678'),
    ('BB024D08-B340-4CE0-B0FD-091C86B17801')
) AS v(StageId);

-- Product pick: uniform across the org's 10 active products.
DECLARE @ProductMap TABLE (SlotIdx INT PRIMARY KEY, ProductId UNIQUEIDENTIFIER);
INSERT INTO @ProductMap (SlotIdx, ProductId)
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, ProductId
FROM (VALUES
    ('F1D5A8A8-1189-45C4-A787-08F7CE153401'), ('6C42C556-444B-4732-A5AF-10E751DC1AE7'), ('B135B848-2CF3-42A7-BE5F-39DF4A38D3FF'),
    ('7B2F76E7-3B3D-49CA-A3AB-4471A18ED3DB'), ('D53986B2-3E1E-4133-AB11-47B379F267CC'), ('3DC7E80B-F9B7-449F-B4E4-6A4C824494F6'),
    ('577C9134-1CE1-46FD-B825-6F7967D81669'), ('606FC49D-7FB4-49F0-BFCC-CE10A1EB8D7A'), ('E3E4D3BF-B11F-40B7-AA23-DC07251CFD93'),
    ('FAA04BC7-1E1C-41C7-99CD-FE5BF5F99DB9')
) AS v(ProductId);

-- Product stage weighting: 10 slots -> Active 7 (70%), Won 2 (20%), Lost 1 (10%).
DECLARE @ProductStageMap TABLE (SlotIdx INT PRIMARY KEY, ProductStageId UNIQUEIDENTIFIER);
INSERT INTO @ProductStageMap (SlotIdx, ProductStageId)
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, StageId
FROM (VALUES
    ('EC8B613A-B56B-4DB0-BA67-CAD8E4C415F4'), ('EC8B613A-B56B-4DB0-BA67-CAD8E4C415F4'), ('EC8B613A-B56B-4DB0-BA67-CAD8E4C415F4'),
    ('EC8B613A-B56B-4DB0-BA67-CAD8E4C415F4'), ('EC8B613A-B56B-4DB0-BA67-CAD8E4C415F4'), ('EC8B613A-B56B-4DB0-BA67-CAD8E4C415F4'),
    ('EC8B613A-B56B-4DB0-BA67-CAD8E4C415F4'),
    ('A7C8B75C-3B97-4BE3-B31F-4D181326D771'), ('A7C8B75C-3B97-4BE3-B31F-4D181326D771'),
    ('50306E34-2407-4081-BB37-F602574A59A6')
) AS v(StageId);

DECLARE @TaskTitles TABLE (Idx INT PRIMARY KEY, Title NVARCHAR(200));
INSERT INTO @TaskTitles (Idx, Title) VALUES
    (0, 'Follow up call'), (1, 'Send proposal'), (2, 'Schedule demo'), (3, 'Send contract'),
    (4, 'Check in email'), (5, 'Qualify budget'), (6, 'Confirm decision maker'),
    (7, 'Renewal discussion'), (8, 'Discovery call'), (9, 'Send pricing sheet'),
    (10, 'Book onboarding session'), (11, 'Collect feedback'), (12, 'Negotiate terms'),
    (13, 'Send case study'), (14, 'Technical deep dive');

;WITH Digits AS (SELECT n FROM (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) AS d(n)),
Numbers AS (
    SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM Digits a CROSS JOIN Digits b CROSS JOIN Digits c CROSS JOIN Digits d
),
FirstNames AS (
    SELECT Name, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx
    FROM (VALUES
        ('James'),('Mary'),('Robert'),('Patricia'),('John'),('Jennifer'),('Michael'),('Linda'),
        ('David'),('Elizabeth'),('William'),('Barbara'),('Richard'),('Susan'),('Joseph'),('Jessica'),
        ('Thomas'),('Sarah'),('Charles'),('Karen'),('Daniel'),('Nancy'),('Matthew'),('Lisa'),
        ('Anthony'),('Margaret'),('Mark'),('Sandra'),('Paul'),('Ashley'),('Steven'),('Kimberly')
    ) AS v(Name)
),
LastNames AS (
    SELECT Name, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx
    FROM (VALUES
        ('Smith'),('Johnson'),('Williams'),('Brown'),('Jones'),('Garcia'),('Miller'),('Davis'),
        ('Rodriguez'),('Martinez'),('Hernandez'),('Lopez'),('Gonzalez'),('Wilson'),('Anderson'),('Thomas'),
        ('Taylor'),('Moore'),('Jackson'),('Martin'),('Lee'),('Perez'),('Thompson'),('White'),
        ('Harris'),('Sanchez'),('Clark'),('Ramirez'),('Lewis'),('Robinson'),('Walker'),('Young')
    ) AS v(Name)
),
Companies AS (
    SELECT Name, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx
    FROM (VALUES
        ('Northwind Traders'),('Globex Industries'),('Initech Solutions'),('Umbrella Logistics'),
        ('Stark Manufacturing'),('Wayne Analytics'),('Wonka Retail'),('Acme Systems'),
        ('Hooli Cloud'),('Soylent Foods'),('Cyberdyne Robotics'),('Vandelay Imports'),
        ('Massive Dynamic'),('Pied Piper Tech'),('Aperture Labs'),('Black Mesa Research'),
        ('Gringotts Finance'),('Oscorp Chemicals'),('Tyrell BioTech'),('Weyland Aerospace')
    ) AS v(Name)
)
SELECT
    NEWID() AS Id,
    fn.Name + ' ' + ln.Name AS Name,
    LOWER(fn.Name) + '.' + LOWER(ln.Name) + CAST(num.n AS VARCHAR(10)) + '@bulkseed2.example.com' AS Email,
    '555-' + RIGHT('0000' + CAST(num.n % 10000 AS VARCHAR(4)), 4) AS Phone,
    co.Name + ' ' + CAST(((num.n - 1) / 20) + 1 AS VARCHAR(10)) AS Company,
    ABS(CHECKSUM(NEWID())) % 20 AS StageSlot,
    ABS(CHECKSUM(NEWID())) % 10 AS ProductIdx,
    ABS(CHECKSUM(NEWID())) % 100 AS ProductRoll,
    ABS(CHECKSUM(NEWID())) % 3 + 1 AS ProductQty,
    ABS(CHECKSUM(NEWID())) % 10 AS ProductStageSlot,
    ABS(CHECKSUM(NEWID())) % 100 AS TaskRoll,
    ABS(CHECKSUM(NEWID())) % 15 AS TaskTitleIdx,
    ABS(CHECKSUM(NEWID())) % 100 AS TaskStatusRoll,
    ABS(CHECKSUM(NEWID())) % 60 AS TaskDueOffset,
    ABS(CHECKSUM(NEWID())) % 2 AS UserIdx,
    ABS(CHECKSUM(NEWID())) % 92 AS DaysAgo,
    DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 92), @Now) AS CreatedAtUtc
INTO #NewLeads
FROM Numbers num
JOIN FirstNames fn ON fn.idx = (num.n - 1) % 32
JOIN LastNames ln ON ln.idx = ((num.n - 1) / 32) % 32
JOIN Companies co ON co.idx = (num.n - 1) % 20
WHERE num.n <= 5000;

INSERT INTO appuser.Leads (Id, OrganizationId, Name, Email, Phone, Company, PipelineStageId, Notes, Source,
                    OwnerUserId, IsActive, CreatedByUserId, CreatedAtUtc, PipelineStageChangedAtUtc)
SELECT
    nl.Id, @OrgId, nl.Name, nl.Email, nl.Phone, nl.Company, sm.PipelineStageId, NULL, 'BulkSeed2',
    CASE WHEN nl.UserIdx = 0 THEN @UserA ELSE @UserB END,
    1,
    CASE WHEN nl.UserIdx = 0 THEN @UserA ELSE @UserB END,
    nl.CreatedAtUtc,
    nl.CreatedAtUtc
FROM #NewLeads nl
JOIN @StageMap sm ON sm.SlotIdx = nl.StageSlot;

-- ~70% of leads get one product line item, at the product's real (current) price.
INSERT INTO appuser.LeadProducts (Id, OrganizationId, LeadId, ProductId, Quantity, UnitPrice, CreatedAtUtc, ProductStageId, StatusChangedAtUtc)
SELECT
    NEWID(), @OrgId, nl.Id, pm.ProductId, nl.ProductQty, COALESCE(p.Price, 0), nl.CreatedAtUtc, psm.ProductStageId, nl.CreatedAtUtc
FROM #NewLeads nl
JOIN @ProductMap pm ON pm.SlotIdx = nl.ProductIdx
JOIN @ProductStageMap psm ON psm.SlotIdx = nl.ProductStageSlot
JOIN appuser.Products p ON p.Id = pm.ProductId AND p.OrganizationId = @OrgId
WHERE nl.ProductRoll < 70;

-- ~75% of leads get one follow-up task; ~30% of those are already Completed.
INSERT INTO appuser.Tasks (Id, OrganizationId, LeadId, Title, Description, DueDate, Status, AssignedToUserId, CreatedByUserId, CreatedAtUtc, CompletedAtUtc)
SELECT
    NEWID(),
    @OrgId,
    nl.Id,
    tt.Title,
    NULL,
    CAST(DATEADD(DAY, nl.TaskDueOffset - 30, nl.CreatedAtUtc) AS DATE),
    CASE WHEN nl.TaskStatusRoll < 30 THEN 'Completed' ELSE 'Open' END,
    CASE WHEN nl.UserIdx = 0 THEN @UserA ELSE @UserB END,
    CASE WHEN nl.UserIdx = 0 THEN @UserA ELSE @UserB END,
    nl.CreatedAtUtc,
    CASE WHEN nl.TaskStatusRoll < 30 THEN DATEADD(DAY, nl.TaskDueOffset - 30, nl.CreatedAtUtc) ELSE NULL END
FROM #NewLeads nl
JOIN @TaskTitles tt ON tt.Idx = nl.TaskTitleIdx
WHERE nl.TaskRoll < 75;

SELECT 'NewLeads' AS Tbl, COUNT(*) AS Cnt FROM appuser.Leads WHERE OrganizationId = @OrgId AND Source = 'BulkSeed2';
SELECT 'NewLeadProducts' AS Tbl, COUNT(*) AS Cnt FROM appuser.LeadProducts WHERE OrganizationId = @OrgId AND LeadId IN (SELECT Id FROM #NewLeads);
SELECT 'NewTasks' AS Tbl, COUNT(*) AS Cnt FROM appuser.Tasks WHERE LeadId IN (SELECT Id FROM #NewLeads);

DROP TABLE #NewLeads;
