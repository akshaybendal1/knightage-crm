# Knightage CRM — SOP

Version 1.0 · 2026-10-26. Shared steps (new server, tenants, backups, incidents) are in
`knightage-deploy/docs/SOP-Operations.md`; this file is what is specific to the CRM.

## 1. Facts

| | |
|---|---|
| URL | https://crm.knightagetech.com (local http://localhost:5105) |
| Code | `src/Knightage.Crm.Api` (.NET 8, Dapper + stored procedures), `client/` (Angular, builds into `wwwroot/browser`) |
| Database | `Knightage_Crm` — 18 tables, 116 procedures, migrations `sql/001`–`054` (`.production.sql` = `appuser` schema) |
| Deploy config | `knightage-deploy/configs/crm.json` → FTP `/crm.knightagetech.com/publish`, preserves `web.config`, `appsettings.json`, `appsettings.Development.json` |
| Android | `client/android`, `com.knightage.crm`, loads the live site; push (Firebase), location, Google sign-in |
| Depends on | Identity (sign-in, team, calendar), Platform (subscription/service status, Inventory URL), Inventory (customer sync), SMTP, Firebase |

## 2. Configuration (server `appsettings.json`)

Template: `knightage-deploy/ops/server/templates/appsettings.crm.json`.

| Key | Purpose | If wrong / missing |
|---|---|---|
| `ConnectionStrings:Default` | `Knightage_Crm` | app fails to start / 500s |
| `Jwt:Key` / `Issuer` / `Audience` | validate Identity's tokens | every call 401 |
| `Services:PlatformBaseUrl` | subscription + service status, Inventory URL lookup | status fails open (everyone active), no Inventory sync |
| `Client:IdentityBaseUrl`, `Client:PlatformBaseUrl` | handed to the browser | sign-in breaks |
| `Smtp:*` | subscription-request and reminder emails | **requests answer "sent" but are only logged — lost** (current production state) |
| `App:PlatformAdminEmail` | who receives subscription requests | 503 on "Request one" |
| `Fcm:ServiceAccountKeyPath` | push notifications | pushes only logged |
| `Google:ClientId` | Google sign-in button | button fails |

## 3. Release procedure

```powershell
cd C:\Akshay\Projects\Knightage\knightage-crm
git status                                   # must be clean and pushed (see §7)
cd client; npx ng build --configuration production; cd ..
Remove-Item -Recurse -Force src\Knightage.Crm.Api\bin\Release\net8.0\publish -ErrorAction SilentlyContinue
cd src\Knightage.Crm.Api; dotnet publish -c Release -o bin/Release/net8.0/publish; cd ..\..
```

1. Migrations first (only if the release has new `sql/NNN_*`): `knightage-deploy\ops\db\Invoke-Migrations.ps1 -App crm
   -Server <sql> -Database Knightage_Crm -Production -WhatIf`, then without `-WhatIf`. Verify the new procs exist.
2. `C:\tools\knightage-deploy\knightage-deploy.exe configs\crm.json --ftp-user <u> --ftp-pass <p> --skip-db --yes`
   (from `knightage-deploy`; ~20 min on the host's FTP; never at the same time as another app).
3. Verify: `ops\checks\Test-Deployment.ps1 -Only crm`; live `main-*.js` name = local `wwwroot/browser/main-*.js`;
   new endpoints answer 401 without a token; one signed-in check (open Leads, add a note).

## 4. Tenant-level administration (inside the CRM, Owner/Admin)

- **Team** (users + roles Owner/Admin/Member/Sales) — invites email a temporary password (Identity).
- **Access Control** — which screens Admin/Member/Sales see. Server-side it is enforced only for Leads export, Products,
  Message templates, Reports/Custom reports and Sales coverage (other screens are hidden in the UI only; see the review).
- **Pipeline and Product Stages** — new orgs get New/Contacted/Qualified/Proposal Sent/Won/Lost and
  Proposed/Approved/Delivered/Cancelled. Several stages may be marked default; field sales uses the lowest sort order.
- **Customers** — a won lead becomes a customer; "Import from won leads" / "Choose leads" converts older won leads;
  customers sync to Inventory automatically (push on create; Inventory → CRM pull at most every 2 minutes per org).
- **Organization / Branding** — name, contact details, logo.
- Field sales screens appear only when the PlatformAdmin turns on Field Force Tracking for the tenant.

## 5. Data tasks

| Task | How |
|---|---|
| Sample data for a demo org | `sql/seed_sample_data.sql` (read it first — it targets one org) |
| Synthetic volume for load tests | `sql/seed_load_test_data.sql` (tags rows `Source='LoadTestSeed'`, removal SQL in the header) — local only |
| Bulk lead import | Leads → Import (CSV: Name, Email, Phone, Company…; max 10 MB) |
| A tenant's status | `ops\tenant\Get-TenantStatus.sql` |

## 6. Troubleshooting

| Symptom | Check |
|---|---|
| Won-lead customers never reach Inventory ("pending") | Platform `Client:ServiceUrls:InventorySales` set? tenant's InventorySales row Provisioned? Customers → "Send to Inventory" |
| Inventory customers missing in CRM | the pull runs when Customers / Add Sales Entry opens, throttled 2 min (3 min after a failure) |
| 402 on every save | subscription in grace period (ReadOnly) — Platform → Tenants |
| 403 `serviceDisabled` | PlatformAdmin disabled CRM for that tenant |
| 503 "Maintenance in progress for this organization" | a tenant migration is running in Platform |
| Reminder pushes never arrive | Firebase key path / file on the server; reminders only run when someone from the org uses the app that day |
| Android: exports do nothing | known: downloads via blob links aren't handled in the WebView (plan item) |

## 7. Known gaps (from the 2026-10-26 review)

Uncommitted working tree since 17 Sep (176 files) — commit and push before the next release. Lead search is slow at
~20k leads (0.6 s, 11 req/s at 25 users); dashboard pipeline summary is uncached. Any role can create/rename pipeline
stages and categories via the API. Deactivated users keep access until their token expires (60 min). See the review
report for the full list and the fix plan.
