-- Captured automatically (browser Geolocation API) when a Field Force Tracking
-- "sales entry" is submitted -- see SalesEntriesController. Always NULL for every
-- other Lead creation path (manual, import, bulk seed).
ALTER TABLE Leads ADD
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    LocationCapturedAtUtc DATETIME2 NULL;
