-- Bulk synthetic data for performance testing (~1000 leads, ~1500-2000 tasks).
-- Tagged so it's identifiable and reversible: Source = 'LoadTestSeed' on every
-- lead, and every email is under @loadtest.example.com. Safe to re-run --
-- skips entirely if LoadTestSeed data already exists (delete first to reseed).
--
-- To remove all of it later:
--   DELETE FROM Tasks WHERE LeadId IN (SELECT Id FROM Leads WHERE Source = 'LoadTestSeed');
--   DELETE FROM Leads WHERE Source = 'LoadTestSeed';

SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM Leads WHERE Source = 'LoadTestSeed')
BEGIN
    PRINT 'LoadTestSeed data already present -- skipping. Delete existing LoadTestSeed rows first if you want to reseed.';
END
ELSE
BEGIN
    -- The one real user in this tenant; every synthetic task is assigned to them.
    DECLARE @UserId NVARCHAR(100) = 'bb7f4f48-5ebe-46de-a78c-8350a6014d55';

    DECLARE @NewLeadIds TABLE (LeadId UNIQUEIDENTIFIER, n INT IDENTITY(1,1));

    ;WITH Digits AS (
        SELECT n FROM (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) AS d(n)
    ),
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
    ),
    StageWeights AS (
        SELECT StageName FROM (VALUES
            ('New'),('New'),('New'),('New'),('New'),('New'),('New'),
            ('Contacted'),('Contacted'),('Contacted'),('Contacted'),('Contacted'),
            ('Qualified'),('Qualified'),('Qualified'),
            ('Proposal Sent'),('Proposal Sent'),
            ('Won'),('Won'),
            ('Lost')
        ) AS v(StageName)
    ),
    StageSlots AS (
        SELECT StageName, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS idx
        FROM StageWeights
    )
    INSERT INTO Leads (Id, Name, Email, Phone, Company, PipelineStageId, Notes, Source, CreatedAtUtc)
    OUTPUT inserted.Id INTO @NewLeadIds(LeadId)
    SELECT
        NEWID(),
        fn.Name + ' ' + ln.Name,
        LOWER(fn.Name) + '.' + LOWER(ln.Name) + CAST(num.n AS VARCHAR(10)) + '@loadtest.example.com',
        '555-' + RIGHT('0000' + CAST(num.n % 10000 AS VARCHAR(4)), 4),
        co.Name + ' ' + CAST(((num.n - 1) / 20) + 1 AS VARCHAR(10)),
        ps.Id,
        NULL,
        'LoadTestSeed',
        DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 365), SYSUTCDATETIME())
    FROM Numbers num
    JOIN FirstNames fn ON fn.idx = (num.n - 1) % 32
    JOIN LastNames ln ON ln.idx = ((num.n - 1) / 32) % 32
    JOIN StageSlots ss ON ss.idx = (num.n - 1) % 20
    JOIN PipelineStages ps ON ps.Name = ss.StageName
    JOIN Companies co ON co.idx = (num.n - 1) % 20
    WHERE num.n <= 1000;

    DECLARE @TaskTitles TABLE (Title NVARCHAR(200), idx INT);
    INSERT INTO @TaskTitles (Title, idx) VALUES
        ('Follow up call', 0), ('Send proposal', 1), ('Schedule demo', 2), ('Send contract', 3),
        ('Check in email', 4), ('Qualify budget', 5), ('Confirm decision maker', 6),
        ('Renewal discussion', 7), ('Discovery call', 8), ('Send pricing sheet', 9),
        ('Book onboarding session', 10), ('Collect feedback', 11), ('Negotiate terms', 12),
        ('Send case study', 13), ('Technical deep dive', 14);

    ;WITH FirstPass AS (
        SELECT LeadId, n, 1 AS seq FROM @NewLeadIds
        UNION ALL
        SELECT LeadId, n, 2 FROM @NewLeadIds WHERE ABS(CHECKSUM(NEWID())) % 100 < 80
    )
    INSERT INTO Tasks (Id, LeadId, Title, Description, DueDate, Status, AssignedToUserId, CreatedByUserId, CreatedAtUtc, CompletedAtUtc)
    SELECT
        NEWID(),
        fp.LeadId,
        tt.Title,
        NULL,
        r.DueDt,
        r.St,
        @UserId,
        @UserId,
        SYSUTCDATETIME(),
        CASE WHEN r.St = 'Completed' THEN SYSUTCDATETIME() ELSE NULL END
    FROM FirstPass fp
    JOIN @TaskTitles tt ON tt.idx = (fp.n + fp.seq) % 15
    CROSS APPLY (
        SELECT
            CAST(DATEADD(DAY, (ABS(CHECKSUM(NEWID())) % 75) - 30, SYSUTCDATETIME()) AS DATE) AS DueDt,
            CASE WHEN ABS(CHECKSUM(NEWID())) % 100 < 30 THEN 'Completed' ELSE 'Open' END AS St
    ) r;

    PRINT 'Seed complete.';
    SELECT 'NewLeads' AS Tbl, COUNT(*) AS Cnt FROM Leads WHERE Source = 'LoadTestSeed';
    SELECT 'NewTasks' AS Tbl, COUNT(*) AS Cnt FROM Tasks WHERE LeadId IN (SELECT LeadId FROM @NewLeadIds);
END
