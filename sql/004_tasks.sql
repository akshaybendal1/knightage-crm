-- Follow-up tasks linked to a lead (User Stories Slice 1, Part B).

CREATE TABLE Tasks (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    LeadId UNIQUEIDENTIFIER NOT NULL REFERENCES Leads(Id),
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000) NULL,
    DueDate DATE NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Open',
    AssignedToUserId NVARCHAR(100) NOT NULL,
    CreatedByUserId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    CompletedAtUtc DATETIME2 NULL
);

CREATE INDEX IX_Tasks_LeadId ON Tasks(LeadId);
CREATE INDEX IX_Tasks_AssignedToUserId_Status ON Tasks(AssignedToUserId, Status);
