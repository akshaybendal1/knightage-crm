# Deploying to IIS

This service ships an API plus the Angular CRM UI (built into
`src/Knightage.Crm.Api/wwwroot/browser`). It never gets its own SQL database
created ahead of time — every tenant's CRM database is provisioned on demand
by `knightage-platform`, so only server + credentials matter here.

## One-time server prerequisites

- **.NET 8 Hosting Bundle** installed on the IIS server — same as every
  Knightage service — https://dotnet.microsoft.com/download/dotnet/8.0,
  "Hosting Bundle" for Windows. Run `iisreset` after installing it.
- **Node.js 20+** wherever you run the Angular build (doesn't need to be the
  IIS server — build elsewhere and copy the output if you prefer).
- `knightage-identity` and `knightage-platform` already deployed and
  reachable from this server (this service calls both).
- A SQL Server login this service can use for the *server + credentials*
  portion of its connection string — it never needs rights on a specific
  database up front, since `TenantResolutionMiddleware` overrides the
  catalog per request. In practice, give it whatever access the tenant
  databases it'll actually be resolved into require (read/write), on the
  same SQL Server instance `knightage-platform` provisions into.

## Build & publish

```powershell
# 1. Angular UI first -- it needs to exist in wwwroot before publish picks it up
cd client
npm install
npm run build   # outputs into ..\src\Knightage.Crm.Api\wwwroot\browser

# 2. API (packages the wwwroot output automatically)
cd ..\src\Knightage.Crm.Api
dotnet publish -c Release -o C:\inetpub\knightage-crm
```

`dotnet publish` auto-generates a working `web.config` (in-process hosting
model) every time — don't hand-edit or check one in.

## IIS site setup

- New Application Pool: .NET CLR Version = **No Managed Code**, Start Mode =
  `AlwaysRunning`.
- New Site with its physical path set to the publish folder above, assigned
  to that app pool, bound to `https://crm.<yourdomain>` with a TLS
  certificate bound in IIS.

## Required configuration (environment variables)

Same mechanism as every Knightage service: `appsettings.json` ships
placeholder values, real ones go on the **Application Pool's environment
variables** (IIS Manager → Application Pools → pool → Advanced Settings →
Environment Variables), which override `appsettings.json` automatically via
ASP.NET Core's `Section__Key` env-var convention.

| Variable | Set to | Notes |
|---|---|---|
| `ConnectionStrings__Default` | `Server=<sql-host>;User Id=<login>;Password=<real-password>;TrustServerCertificate=True` | the `Database=` segment is optional/ignored — see prerequisites above |
| `Jwt__Key` | same value as `knightage-identity`'s `Jwt__Key` | **must match byte-for-byte** — this service only validates tokens |
| `Jwt__Issuer` | `knightage-identity` | must match identity exactly |
| `Jwt__Audience` | `knightage-platform-clients` | must match identity exactly |
| `Services__PlatformBaseUrl` | `https://platform.<yourdomain>` | resolves each request's tenant database — every request 503s without this reachable |
| `Client__IdentityBaseUrl` | `https://identity.<yourdomain>` | read by the Angular app at startup via `GET /api/client-config`; wrong value breaks login for every user |
| `Cors__AllowedOrigins__0` | `https://crm.<yourdomain>` | mostly moot in production — the SPA is served same-origin from this same API, so it doesn't actually cross-origin call itself. Kept configured for a future second client (e.g. a mobile app) hitting this API directly. |

## Database

Nothing to run by hand here. Each organization's CRM database gets created
automatically by `knightage-platform` at registration time, using its
bundled `schemas/crm.sql` — **make sure that file is current** (see
`knightage-platform`'s own `DEPLOYMENT.md`) before onboarding real tenants,
since a CRM migration added here doesn't take effect for new tenants until
it's mirrored there.

Never run `sql/seed_sample_data.sql` against a real tenant's database — it's
local-testing-only fake data.

## Verify

`/swagger` is disabled outside Development. Load `https://crm.<yourdomain>/login`
in a browser and confirm the page renders (proves the Angular build shipped
correctly) and that submitting the login form reaches `knightage-identity`
rather than erroring immediately (proves `Client__IdentityBaseUrl` and CORS
on the identity side are both correct). If the API itself doesn't come up,
check `logs\stdout` in the publish folder (enable `stdoutLogEnabled="true"`
in the generated `web.config` temporarily while diagnosing, then turn it
back off).
