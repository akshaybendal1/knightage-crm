-- Optional sample/demo data for local testing -- NOT part of tenant provisioning
-- (deliberately not copied into knightage-platform's schemas/crm.sql, so real
-- organizations never get seeded with these fake leads). Run by hand against a
-- specific tenant's Crm database when you want a populated UI to click through
-- instead of an empty one. Safe to re-run: skips stages/leads that already exist.

INSERT INTO PipelineStages (Id, Name, SortOrder, IsActive)
SELECT NEWID(), s.Name, s.SortOrder, 1
FROM (VALUES
    ('New', 1),
    ('Contacted', 2),
    ('Qualified', 3),
    ('Proposal Sent', 4),
    ('Won', 5),
    ('Lost', 6)
) AS s(Name, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM PipelineStages p WHERE p.Name = s.Name);

DECLARE @New UNIQUEIDENTIFIER = (SELECT Id FROM PipelineStages WHERE Name = 'New');
DECLARE @Contacted UNIQUEIDENTIFIER = (SELECT Id FROM PipelineStages WHERE Name = 'Contacted');
DECLARE @Qualified UNIQUEIDENTIFIER = (SELECT Id FROM PipelineStages WHERE Name = 'Qualified');
DECLARE @Proposal UNIQUEIDENTIFIER = (SELECT Id FROM PipelineStages WHERE Name = 'Proposal Sent');
DECLARE @Won UNIQUEIDENTIFIER = (SELECT Id FROM PipelineStages WHERE Name = 'Won');
DECLARE @Lost UNIQUEIDENTIFIER = (SELECT Id FROM PipelineStages WHERE Name = 'Lost');

INSERT INTO Leads (Id, Name, Email, Phone, Company, PipelineStageId, Notes, Source, CreatedAtUtc)
SELECT NEWID(), l.Name, l.Email, l.Phone, l.Company, l.StageId, l.Notes, l.Source, SYSUTCDATETIME()
FROM (VALUES
    ('Grace Hopper', 'grace.hopper@example.com', '555-0101', 'Naval Systems Inc', @New, CAST(NULL AS NVARCHAR(1000)), 'Manual'),
    ('Alan Turing', 'alan.turing@example.com', '555-0102', 'Bletchley Analytics', @New, NULL, 'Import'),
    ('Margaret Hamilton', 'margaret.hamilton@example.com', '555-0103', 'Apollo Software Co', @Contacted, 'Left a voicemail, following up Thursday.', 'Manual'),
    ('Katherine Johnson', 'katherine.johnson@example.com', '555-0104', 'Orbital Dynamics', @Contacted, NULL, 'Manual'),
    ('Tim Berners-Lee', 'tim.bl@example.com', '555-0105', 'Web Foundation', @Qualified, 'Budget confirmed, needs board sign-off.', 'Manual'),
    ('Ada King', 'ada.king@example.com', '555-0106', 'Analytical Engines Ltd', @Proposal, 'Sent proposal, awaiting response.', 'Import'),
    ('John von Neumann', 'jvn@example.com', '555-0107', 'IAS Computing', @Won, 'Signed annual contract.', 'Manual'),
    ('Barbara Liskov', 'barbara.liskov@example.com', '555-0108', 'MIT Ventures', @Lost, 'Went with a competitor on pricing.', 'Manual')
) AS l(Name, Email, Phone, Company, StageId, Notes, Source)
WHERE NOT EXISTS (SELECT 1 FROM Leads x WHERE x.Email = l.Email);

INSERT INTO LeadActivities (Id, LeadId, Type, Content, CreatedByUserId, CreatedAtUtc)
SELECT NEWID(), a.LeadId, 'Note', a.Content, NULL, SYSUTCDATETIME()
FROM (
    SELECT Id AS LeadId, 'Initial outreach email sent.' AS Content FROM Leads WHERE Email = 'margaret.hamilton@example.com'
    UNION ALL
    SELECT Id, 'Called and left a voicemail about the enterprise tier.' FROM Leads WHERE Email = 'margaret.hamilton@example.com'
    UNION ALL
    SELECT Id, 'Proposal sent, following up in one week.' FROM Leads WHERE Email = 'ada.king@example.com'
) AS a
WHERE NOT EXISTS (
    SELECT 1 FROM LeadActivities x WHERE x.LeadId = a.LeadId AND x.Content = a.Content
);
