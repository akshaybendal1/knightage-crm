# Knightage CRM

Lead/contact management system for the Knightage platform: leads move through a user-configurable pipeline (New → Contacted → Qualified → Won/Lost, or whatever stages an org defines), created manually or bulk-imported from a CSV.

This is part of the Knightage multi-system platform:

- knightage-identity — Auth/SSO
- knightage-platform — tenant control plane
- knightage-doc-intelligence — document extraction (OCR/NER) service
- knightage-accounting — Phase 1 business system
- **knightage-crm** (this repo) — Phase 2 business system
- knightage-inventory-sales — business system (built in a later phase)

## Status

Backend and Angular frontend built together in one pass (unlike `knightage-accounting`, where the frontend was added after the fact — see that repo's history for why that split is worth avoiding).

- Pipeline stages — CRUD, ordered by `SortOrder`, mirrors how `knightage-accounting`'s `AccountingRule` made categories configurable instead of hardcoding an enum.
- Leads — a single entity covering both "lead" and "contact" (per the platform's original "Lead Management/CRM" framing), not split into separate Leads/Contacts/Companies/Deals tables.
  - `GET /api/leads` (optional `pipelineStageId` filter), `GET /api/leads/{id}`, `POST /api/leads` (manual create), `PUT /api/leads/{id}` (edit fields, move pipeline stage).
  - `POST /api/leads/import` (multipart: `file` + `pipelineStageId`) bulk-imports a CSV (`Name,Email,Phone,Company`) using the same hand-rolled quote-aware CSV parsing approach as `knightage-accounting`'s bank statement importer. Imported leads are tagged `Source: Import` and land in whichever stage the caller specifies.
- JWT bearer authentication, validating tokens issued by `knightage-identity` — same shared-secret pattern as every other business service. Every business endpoint is `[Authorize]`-protected.
- `GET /api/client-config` (anonymous) serves `{ identityBaseUrl }` from `appsettings.json`, fetched by the Angular app on startup — built in from day one this time.
- Angular UI (`client/`) built into this API's `wwwroot`, same single-deployable-unit pattern as Accounting.

**Deferred, explicitly out of scope for this pass**: business-card/scanned-contact OCR import via `knightage-doc-intelligence`. That would need a new `ContactList` `IDocumentExtractor` registered in the extraction sidecar — the same shape of follow-up slice that invoice OCR capture was for Accounting, after its basic CRUD landed first. CSV import (already-digital contact lists) covers the near-term need; photographed business cards do not go through this yet.

## Project layout

- `src/Knightage.Crm.Api` — Web API host (controllers, startup, config); serves the built Angular app from `wwwroot/browser`
- `src/Knightage.Crm.Core` — domain models and interfaces
- `src/Knightage.Crm.Infrastructure` — data access (Dapper + SQL Server)
- `src/Knightage.Crm.Service` — the CSV lead-import parser
- `client/` — the Angular app (standalone components, signals, functional guards/interceptors, same generator flow as Accounting's)

## Data model

`sql/001_init.sql` creates `PipelineStages` and `Leads`. Run it by hand against your local SQL Server instance until `knightage-platform`'s migration orchestration takes over.

## Auth

This service does not issue tokens — it only validates JWTs issued by `knightage-identity`. `appsettings.json`'s `Jwt:Key`/`Issuer`/`Audience` must match `knightage-identity`'s exactly (shared HMAC secret for now; revisit before this crosses a real network boundary in production).

## Client runtime config

Same pattern as `knightage-accounting`: the Angular app doesn't hardcode `knightage-identity`'s URL. It calls `GET /api/client-config` on startup and gets `{ identityBaseUrl }`, read server-side from `appsettings.json`'s `Client:IdentityBaseUrl`.

## Running locally

Requires the .NET 8 SDK, Node.js 20+, and a local SQL Server instance.

```
# 1. Build the Angular app first -- it needs to exist in wwwroot before dotnet run
cd client
npm install
npm run build   # runs `ng build`, outputs to ../src/Knightage.Crm.Api/wwwroot
cd ..

# 2. Build the backend
dotnet build
```

Update `src/Knightage.Crm.Api/appsettings.json`:
- `ConnectionStrings:Default` — point at your local SQL Server, after running `sql/001_init.sql` against it.
- `Jwt:Key`/`Issuer`/`Audience` — must match the values configured in `knightage-identity`.
- `Client:IdentityBaseUrl` — where `knightage-identity` is running (defaults to `http://localhost:5101`).

```
dotnet run --project src/Knightage.Crm.Api
```

Then open `http://localhost:5105` for the full UI. Swagger UI is separately available at `/swagger` in development.

### Try it

```
# 1. Create pipeline stages
curl -X POST http://localhost:5105/api/pipeline-stages -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"name":"New","sortOrder":1}'
curl -X POST http://localhost:5105/api/pipeline-stages -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"name":"Qualified","sortOrder":2}'

# 2. Create a lead manually
curl -X POST http://localhost:5105/api/leads -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"name":"Ada Lovelace","email":"ada@example.com","company":"Acme Co","pipelineStageId":"<new-stage-id>"}'

# 3. Bulk import a CSV
curl -X POST http://localhost:5105/api/leads/import \
  -H "Authorization: Bearer <token>" \
  -F "pipelineStageId=<new-stage-id>" \
  -F "file=@leads.csv"
```

Where `leads.csv` looks like:

```
Name,Email,Phone,Company
Grace Hopper,grace@example.com,555-0100,Example Corp
Alan Turing,alan@example.com,,Example Corp
```
