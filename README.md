# ballware document service
[![Build & Publish (main)](https://github.com/ballware/ballware-document-service/actions/workflows/publish-packages.yml/badge.svg?branch=main)](https://github.com/ballware/ballware-document-service/actions/workflows/publish-packages.yml)
[![CI Tests (main)](https://github.com/ballware/ballware-document-service/actions/workflows/sonarqube-latest.yml/badge.svg?branch=main)](https://github.com/ballware/ballware-document-service/actions/workflows/sonarqube-latest.yml)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=ballware_ballware-document-service&metric=coverage)](https://sonarcloud.io/summary/overall?id=ballware_ballware-document-service)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=ballware_ballware-document-service&metric=bugs)](https://sonarcloud.io/summary/overall?id=ballware_ballware-document-service)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=ballware_ballware-document-service&metric=vulnerabilities)](https://sonarcloud.io/summary/overall?id=ballware_ballware-document-service)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=ballware_ballware-document-service&metric=security_rating)](https://sonarcloud.io/summary/overall?id=ballware_ballware-document-service)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=ballware_ballware-document-service&metric=reliability_rating)](https://sonarcloud.io/summary/overall?id=ballware_ballware-document-service)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=ballware_ballware-document-service&metric=sqale_rating)](https://sonarcloud.io/summary/overall?id=ballware_ballware-document-service)

Multi-tenant document and reporting service for the ballware platform. It stores report
definitions (DevExpress XtraReports), exposes them as REST APIs, hosts an interactive Blazor
report **designer** and **viewer**, and runs background jobs that render reports to PDF and
deliver them by e-mail according to user subscriptions.

The repository builds two deployable ASP.NET Core 10 hosts from one shared code base:

| Host | Role | Auth | Default dev port |
| --- | --- | --- | --- |
| `Ballware.Document.Service` | Headless REST API + Quartz scheduler + reporting engine | JWT Bearer only | `5004` |
| `Ballware.Document.App` | Blazor Web App with `DxReportDesigner` / `DxDocumentViewer` | Session cookie via OIDC sign-on | `5005` |

Both hosts consume the same configuration contract and the same EF Core data store, so the API
and the UI stay consistent without a service-to-service call for report metadata.

- License: MIT
- Target framework: `.NET 10` (`net10.0`)
- Repository: <https://github.com/ballware/ballware-document-service>

---

## 1. Architecture

### 1.1 Component view

```
                       ┌──────────────────────────────────────────────┐
   Browser / SPA ─────▶│ Ballware.Document.Service   (API host)       │
   (Bearer JWT)        │  • Minimal-API endpoints /document/**        │
                       │  • Swagger UI (/swagger, 2 API groups)       │
                       │  • Quartz clustered scheduler + jobs         │
                       │  • DevExpress reporting core (SkiaSharp PDF) │
                       └───────────┬──────────────────┬───────────────┘
                                   │                  │
   Browser ───────────▶ ┌──────────▼─────────┐       │
   (session cookie)     │ Ballware.Document.App│      │
                        │  • /signon/{idToken} │      │
                        │  • /designer ?id=…   │      │
                        │  • /viewer  ?<report>│      │
                        │  • Interactive Server│      │
                        └──────────┬───────────┘      │
                                   │                  │
                       ┌───────────▼──────────────────▼───────────────┐
                       │ Ballware.Document.Hosting (shared)           │
                       │  Options · DI extensions · adapters · maps   │
                       └───┬───────────┬──────────────┬───────────────┘
                           │           │              │
              ┌────────────▼──┐  ┌──────▼───────┐  ┌──▼──────────────────────┐
              │ Data + EF Core│  │ Platform     │  │ Engine.Dx               │
              │ Postgres /    │  │ services via │  │ DevExpress reporting,   │
              │ SQL Server    │  │ OAuth2 CC    │  │ datasources, mail, PDF  │
              └───────────────┘  └──────────────┘  └─────────────────────────┘
                                 meta · storage · generic · ml
```

### 1.2 Design rules

1. **Shared hosting layer.** `Ballware.Document.Hosting` contains all options binding, DI
   extension methods, Mapster profiles and client adapters. Browser authentication (cookie +
   OpenID Connect + session) is not implemented here — it is provided by the shared packages
   `Ballware.Shared.Web.Oidc` / `ballware.shared.web.session`, which `Hosting` wires up. Neither
   host references the other; both reference `Hosting`.
2. **API host is JWT-only.** Razor components, antiforgery/CSP framing and the session cookie
   live in the App host only. The Service host sets `DefaultScheme = DefaultChallengeScheme =
   JwtBearer`.
3. **Direct data access from the UI host.** The App host uses the same EF Core repositories and
   `IDocumentDbConnectionFactory`, so the designer resolves report storage and datasource
   metadata in-process. `Storage:AutoMigrations` must stay `false` for the App — migrations are
   owned by the Service host.
4. **Provider-independent persistence.** `Storage:Provider` selects `postgres` or `mssql`
   at startup; both provider assemblies ship in the same solution with their own migration
   sets (`EFCore.NamingConventions` for consistent snake_case naming).
5. **Engine split.** `Ballware.Document.Engine.Dx` is a Razor class library whose extensions are
   divided into `AddBallwareDevExpressReportingCore()` / `UseBallwareDevExpressReportingCore()`
   (Service: rendering, datasources, mail) and `AddBallwareDevExpressReportingBlazor()` /
   `UseBallwareDevExpressReportingBlazor()` (App: Blazor reporting, `ReportStorageWebExtension`).
6. **Multi-tenancy everywhere.** Tenant, user and right claims are resolved from the token via
   `ballware.shared.authorization` (`tenant`, `sub`, `right` by default). All entities are
   tenantable repositories, and every endpoint and job is tenant-scoped.

### 1.3 Project layout

| Project | Responsibility |
| --- | --- |
| `Ballware.Document.Service` | API host composition (`Program.cs`, `Startup.cs`), Swagger docs, Quartz host, import/export repository hook |
| `Ballware.Document.App` | Blazor Web App composition; `/signon`, challenge-differentiation and sign-in fallback middleware come from `Ballware.Shared.Web.Oidc` / `ballware.shared.web.session` |
| `Ballware.Document.Api` | Minimal-API endpoint groups for document / notification / subscription / processing-state |
| `Ballware.Document.Hosting` | Options, validation, authentication wiring (shared browser auth, JWT-only API auth), service clients, datastore selection, metadata providers, adapters, Mapster profiles |
| `Ballware.Document.Engine.Dx` | DevExpress reporting integration, report storage, datasource provider, mail/PDF generation, DE localization assets |
| `Ballware.Document.Jobs` | Quartz job and trigger registration (`MailOptions`, `TriggerOptions`) |
| `Ballware.Document.Data` | Public contracts and persistables (`Document`, `Notification`, `Subscription`), repository interfaces, select-list DTOs |
| `Ballware.Document.Data.Ef` | DbContext abstraction, model customization, shared mapping profile |
| `Ballware.Document.Data.Ef.Postgres` | Npgsql provider + migrations |
| `Ballware.Document.Data.Ef.SqlServer` | Microsoft SQL Server provider + migrations |
| `Ballware.Document.Metadata` | Provider contracts (`IDocumentMetadataProvider`, `IDocumentModificationProvider`, `IDocumentMailGenerator`, `IDatasourceDefinitionProvider`, …) |
| `test/*` | NUnit suites for API, authorization and both EF providers |

---

## 2. Technology

| Concern | Choice |
| --- | --- |
| Runtime / web stack | .NET 10, ASP.NET Core (Kestrel), Minimal APIs + `AddControllers`, Razor Components (Interactive Server) |
| Reporting | DevExpress Reporting 26.1 — `DevExpress.AspNetCore.Reporting`, `DevExpress.Blazor.Reporting.JSBasedControls`, `DevExpress.Drawing`/`Drawing.Skia`, `DevExpress.Pdf.SkiaRenderer`, `DevExpress.Blazor` + Fluent theme |
| Image/PDF rendering | SkiaSharp (`SkiaSharp.NativeAssets.Linux`), Microsoft core fonts installed in the images |
| Persistence | EF Core 10, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.SqlServer`, `EFCore.NamingConventions` |
| Scheduling | Quartz.NET 3.8 (`Quartz.AspNetCore`, clustered AdoJobStore with JSON serialization) |
| Authentication | `Microsoft.AspNetCore.Authentication.JwtBearer`, `OpenIdConnect`, Cookie; `Ballware.Shared.Web.Oidc` (browser sign-in, `/signon`, challenge fallback), `ballware.shared.web.session` (distributed session), ASP.NET Core Data Protection |
| Service-to-service auth | `Duende.AccessTokenManagement` client-credentials token management |
| Platform clients | `ballware.meta.service.client`, `Ballware.Storage.Service.Client`, `Ballware.Generic.Service.Client`, `Ballware.Ml.Service.Client` |
| Shared ballware building blocks | `ballware.shared.api.endpoints`, `Ballware.Shared.Api.Jobs`, `Ballware.Shared.Data`, `ballware.shared.data.ef`, `Ballware.Shared.Authorization(.Jint)`, `Ballware.Shared.Web.Oidc`, `ballware.shared.web.session` |
| Mapping | Mapster + `Mapster.DependencyInjection` |
| Logging | Serilog (`Serilog.AspNetCore`, console sink), `UseSerilogRequestLogging` |
| API docs | Swashbuckle 10 (`Swashbuckle.AspNetCore.Annotations`), `Swashbuckle.AspNetCore.Cli`, `NSwag.ConsoleCore` |
| Testing | NUnit 4, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.TestHost`, Moq, coverlet (OpenCover format) |
| Versioning | GitVersion 6.3 (continuous deployment mode) |
| Packaging | NuGet to GitHub Packages, Docker images to GHCR (`linux/amd64`, `linux/arm64`) |
| Quality gate | SonarQube scanner with OpenCover coverage upload |

Private NuGet feeds are mandatory: `nuget.pkg.github.com/ballware` for the `Ballware.*`
packages and the DevExpress NuGet feed for the reporting packages (a DevExpress license is
required at build time).

---

## 3. Features

### Documents and report definitions
- Documents are stored as tenant-owned entities with `DisplayName`, `Entity` binding, `State`,
  `ReportBinary` (serialized XtraReport) and `ReportParameter`.
- Full CRUD through the generic tenantable editing API (`MapTenantableEditingApi`) with
  import/export hooks (`DocumentImportExportRepositoryHook`) for portable document exchange.
- `DocumentUpdateDatasourcesJob` refreshes the datasource definitions of a report so that
  changed platform metadata is reflected without re-opening the designer.

### Interactive designer and viewer (App host)
- `/designer?id=<documentId>` — `DxReportDesigner` with custom SQL enabled (`EnableCustomSql`),
  per-user datasource editing rights (`meta.document.edit`) and injected data sources from the
  meta/generic/ML services.
- `/viewer?<query>` — `DxDocumentViewer` rendering the report to PDF/Skia in-process.
- Designer UI is customized client-side via `wwwroot/customization.js`
  (`BallwareReportingCustomization.customizeMenuActions` / `customizeLocalization`).
- German localization bundles for the JS controls are shipped as embedded static assets under
  `wwwroot/Localization/` (`dx-reporting.de.json`, `dx-rich.de.json`, `dx-analytics-core.de.json`);
  request culture is pinned to `de-DE`.
- Embedded reporting functions usable from report expressions and criteria:
  `FetchNameByIdFromLookup`, `FetchTextByValueFromPickvalue`, `FetchNameByStateFromProcessingState`.

### Notifications, subscriptions and mail delivery
- `Notification` is the delivery definition: `Identifier`, `Name`, `State`, an optional bound
  `DocumentId` and `DocumentParams` for parameterized report output.
- `Subscription` stores recipient user/e-mail, body, attachment flag and file name, frequency,
  active flag and the last send result (`LastSendStamp`, `LastError`).
- `SubscriptionTriggerJob` renders the document to PDF and sends it over SMTP;
  `SubscriptionFrequencyJob` schedules subscriptions per frequency from cron triggers.
- Sending is restricted to `Mail:AllowedTenants`.

### Processing states
- `MapProcessingStateDocumentApi` exposes the allowed successor states for document and
  notification instances, so callers can drive state transitions consistently.

### Background infrastructure
- Quartz runs clustered with a persistent AdoJobStore (PostgreSQL delegate by default), so
  multiple Service instances share the schedule; the scheduler waits for jobs on shutdown.
- Durable jobs are registered per entity for tenantable import
  (`document`, `notification`, `subscription`).

### API surface
- Two OpenAPI documents: **ballware document user API** (scope `documentApi`) and **ballware
  document service API** (scope `serviceApi`), with OAuth2/OIDC discovery, PKCE-enabled Swagger UI.
- Endpoint groups (all under `/document/...`):

| Path | Audience | Representative operations |
| --- | --- | --- |
| `/document/document` | user | `selectlist`, `selectlistdocumentsforentity/{entity}`, `selectbyid/{id}`, `updatedatasources`, tenantable CRUD |
| `/document/document` | service | `selectlistdocumentsfortenant/{tenantId}`, `documentmetadatabytenantandid/{tenantId}/{id}`, `documenttemplatebehalfofuserbytenant/{tenantId}` |
| `/document/notification` | user / service | `selectlist`, `selectbyid/{id}`, `notificationmetadatabytenantandid/...`, `notificationmetadatabytenantandidentifier/...` |
| `/document/subscription` | user / service | `selectlist`, `selectbyid/{id}`, `trigger`, tenantable CRUD |
| `/document/processingstate` | user | `selectlistallowedsuccessorsforentities/document`, `.../notification` |

All `/document/**` user operations (generic CRUD included) are contained in the **`document`**
OpenAPI document; the inter-service operations form the **`service`** document. Committed
snapshots and a generator script live in [docs/openapi/](docs/openapi/).

---

## Documentation

- [docs/README.md](docs/README.md) – documentation index (concepts + API reference)
- [docs/api/README.md](docs/api/README.md) – API surface and conventions
- [docs/openapi/](docs/openapi/) – committed OpenAPI snapshots (`document.json`, `service.json`) and how to regenerate them

---

## 4. Configuration

### 4.1 Precedence

Both hosts clear the default configuration sources and load, in order:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. `appsettings.local.json` (git-ignored, machine-local overrides)
4. Environment variables (`Section__SubSection` notation)

Required sections fail fast at startup with a `ConfigurationException` (options are bound with
`ValidateOnStart` + data annotations) and the process exits with code `-1`.

### 4.2 Authorization

```jsonc
"Authorization": {
  "Authority": "https://identity.ballware.de/realms/ballware",
  "Audience": "metaApi",
  "ClientId": "document",
  "TenantClaim": "tenant",
  "UserIdClaim": "sub",
  "RightClaim": "right",
  "Issuer": null,                 // defaults to Authority
  "RequireHttpsMetadata": true,
  "RequiredUserScope": "documentApi",
  "UserScopePolicyName": "documentApi",        // App host only
  "ServiceScopePolicyName": "serviceApi",      // App host only
  "SignOnAllowedHosts": [ "reporting.ballware.de" ],   // App host only
  "SignOnAllowedRedirectPathPrefixes": [ "/viewer", "/designer" ], // App host only
  "PublicBaseUrl": "https://reporting.ballware.de"     // App host only
}
```

The section is bound twice on the App host: `Ballware.Shared.Web.Oidc` reads it as
`BrowserAuthorizationOptions` (browser sign-in, `/signon`, policies), `Hosting` reads the document
subset (`Authority`, `Audience`, `Issuer`, `RequireHttpsMetadata`, `RequiredUserScope`, claims) for
the API host and tenant evaluation.

- `Authority`, `Audience`, `ClientId` are required.
- Authorization policies: `documentApi` (user scope from the `scope` claim) and `serviceApi`
  (`serviceApi` scope). Both assert on JWT bearer. The App host names them through
  `UserScopePolicyName` / `ServiceScopePolicyName` (defaults to `RequiredUserScope` / no service
  policy); the Service host always registers `documentApi` and `serviceApi`.
- `PublicBaseUrl` makes the OIDC `redirect_uri` deterministic (`PublicBaseUrl + CallbackPath`);
  it must be registered as a valid redirect URI for `ClientId` at the identity provider. When it
  is omitted the redirect is derived from the incoming request and a warning is logged.
- `SignOnAllowedHosts` restricts the hosts that `/signon/{idToken}` may redirect back to,
  `SignOnAllowedRedirectPathPrefixes` the paths it may redirect to (`/` is always allowed).

### 4.3 Storage

```jsonc
"Storage": {
  "Provider": "postgres",          // "postgres" | "mssql"
  "ConnectionString": "document",  // name of a ConnectionStrings entry
  "AutoMigrations": true           // Service host only; keep false for the App host
},
"ConnectionStrings": {
  "document": "Host=localhost;Port=5432;Database=ballware_document;Username=...;Password=..."
}
```

An unknown provider or a missing connection string aborts startup.

### 4.4 Platform service clients

Each client needs `ServiceUrl`, `TokenEndpoint`, `ClientId`, `ClientSecret`, `Scopes` and is used
with OAuth2 client credentials via Duende token management. All four blocks are mandatory:

| Section | Used for |
| --- | --- |
| `MetaClient` | entity metadata, pickvalues, datasources, processing states, jobs/exports, authorization metadata |
| `StorageClient` | file storage provider (`IFileStorageProvider`) |
| `GenericClient` | document lookup and generic datasource definitions |
| `MlClient` | ML datasource definitions |

```jsonc
"MetaClient": {
  "ServiceUrl": "https://localhost:6001",
  "TokenEndpoint": "https://identity.ballware.de/realms/ballware/protocol/openid-connect/token",
  "ClientId": "meta",
  "ClientSecret": "<secret>",
  "Scopes": "openid serviceApi offline_access"
}
```

Environment-variable form (as used by `ballware-document-service.env`):
`MetaClient__ServiceUrl`, `MetaClient__ClientId`, `StorageClient__Scopes`, …

### 4.5 Session and data protection (App host)

```jsonc
"Session": {
  "EnableSession": true,
  "SessionCookieName": ".BallwareDocumentSession",
  "SessionCacheKeyPrefix": "session:",
  "SessionCacheExpirationInMinutes": 60,
  "SuppressDefaultSessionCookie": true,
  "DataProtectionPurpose": "Ballware.Document.Session.ClaimsPrincipal"
},
"DataProtection": { "KeysPath": "/keys/document-app" }
```

- App host: cookie scheme as default, `OpenIdConnect` as default challenge, JWT bearer available
  for direct bearer requests (all registered by `Ballware.Shared.Web.Oidc`). Data protection is
  configured for the App host only, with its own application name (`ballware-document-app`);
  `KeysPath` is optional — without it keys stay in-process (single instance).
- App antiforgery cookie is `.Ballware.Antiforgery`. `X-Frame-Options` is suppressed
  automatically when `ContentSecurity:FrameAncestors` allows foreign framing.
- Sign-on: `GET /signon/{idToken}?redirect=…` (from `Ballware.Shared.Web.Oidc`) validates the id
  token against the IdP signing keys (`ValidAudience = ClientId`), establishes the session and
  redirects. `redirect` must be relative and start with `/` or one of
  `Authorization:SignOnAllowedRedirectPathPrefixes`, otherwise `400`.

### 4.6 Framing and CORS

```jsonc
// App host
"ContentSecurity": { "FrameAncestors": "self https://app.ballware.de" }

// Service host
"Cors": {
  "AllowedOrigins": "https://app.ballware.de",
  "AllowedMethods": "GET POST PUT DELETE",
  "AllowedHeaders": "Authorization Content-Type"
}
```

The whole CORS section is optional; when it is absent no CORS policy is registered.

`FrameAncestors` accepts `self`, `none` or `self <origins…>`; it is normalized into the CSP
`frame-ancestors` policy of the Razor component endpoints. Defaults to `'self'`.

### 4.7 Mail (Service host, required)

```jsonc
"Mail": {
  "FromName": "ballware reporting",
  "FromMail": "reporting@example.com",
  "SmtpHost": "smtp.example.com",
  "SmtpPort": 587,
  "SmtpUser": "<user>",
  "SmtpPassword": "<password>",
  "SmtpUseSsl": true,
  "SmtpTimeout": 20000,
  "AllowedTenants": [ "00000000-0000-0000-0000-000000000000" ]
}
```

### 4.8 Triggers and Quartz (Service host)

```jsonc
"Trigger": {
  "Active": [
    { "Name": "daily",   "Cron": "0 0 6 * * ?",  "Frequency": 1 },
    { "Name": "weekly",  "Cron": "0 0 7 ? * MON", "Frequency": 7 }
  ]
},
"Quartz": {
  "quartz.scheduler.instanceId": "AUTO",
  "quartz.serializer.type": "json",
  "quartz.jobStore.type": "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
  "quartz.jobStore.driverDelegateType": "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz",
  "quartz.jobStore.clustered": true
}
```

Triggers are bound to `SubscriptionFrequencyJob` in the `subscription` group; add the matching
AdoJobStore delegate for SQL Server.

### 4.9 Swagger (Service host, optional)

```jsonc
"Swagger": {
  "EnableClient": true,
  "ClientId": "swagger",
  "ClientSecret": "<secret>",
  "RequiredScopes": "openid documentApi serviceApi"
}
```

If the section is absent, endpoint exploration stays but no Swagger UI/JSON is exposed.
`RequiredScopes` defaults to `openid documentApi`.

### 4.10 Kestrel and logging

Both hosts raise request limits for large report payloads
(`MaxRequestBodySize` / `MaxRequestBufferSize` / `MaxRequestHeadersTotalSize` = 40 000 000 bytes,
`MaxRequestLineSize` = 24 576) and configure Serilog with the console sink,
`FromLogContext` + `WithMachineName` enrichment, and `Warning` overrides for `Microsoft`/`System`.
Development profiles enable `DetailedErrors` and pin HTTP endpoints to `http://localhost:5004`
(Service) and `http://localhost:5005` (App). Forwarded headers (`X-Forwarded-For`,
`X-Forwarded-Proto`) are always applied for reverse-proxy deployments.

---

## 5. Build and run

### 5.1 Prerequisites

- .NET 10 SDK
- Access to the `ballware` GitHub NuGet feed (`GITHUB_TOKEN` with `read:packages`)
- A DevExpress NuGet feed URL and a DevExpress license file (Reporting 26.1)

```bash
dotnet nuget add source "https://nuget.pkg.github.com/ballware/index.json" \
  --name github --username "$USER" --password "$GITHUB_TOKEN" --store-password-in-clear-text
dotnet nuget add source "$DEVEXPRESS_NUGETFEED" --name devexpress

mkdir -p "$HOME/.config/DevExpress"
printf '%s' "$DEVEXPRESS_LICENSE" > "$HOME/.config/DevExpress/DevExpress_License.txt"

dotnet restore ballware-document-service.sln
dotnet build ballware-document-service.sln -c Release
```

### 5.2 Run locally

```bash
cp ballware-document-service.env .env   # fill in client secrets + connection strings
dotnet run --project src/Ballware.Document.Service   # http://localhost:5004
dotnet run --project src/Ballware.Document.App       # http://localhost:5005
```

Interactive API docs for the Service host: `http://localhost:5004/swagger`.
UI routes of the App host: `/signon/{idToken}`, `/designer?id=<documentId>`, `/viewer?<report>`.

### 5.3 Docker

```bash
docker build -f src/Ballware.Document.Service/Dockerfile src/. \
  --build-arg GITHUB_USERNAME=... --build-arg GITHUB_PAT=... \
  --build-arg DEVEXPRESS_NUGETFEED=... --build-arg DEVEXPRESS_LICENSE=... \
  -t ballware-document-service:local

docker build -f src/Ballware.Document.App/Dockerfile src/. \
  --build-arg GITHUB_USERNAME=... --build-arg GITHUB_PAT=... \
  --build-arg DEVEXPRESS_NUGETFEED=... --build-arg DEVEXPRESS_LICENSE=... \
  -t ballware-document-app:local
```

Images are based on `mcr.microsoft.com/dotnet/aspnet:10.0`, install ICU, fontconfig, JPEG
libraries and the Microsoft core fonts (required for report rendering), run as the non-root `app`
user, expose `5000`/`5001`, and fail the build when the DevExpress feed or license arguments are
missing.

Published images: `ghcr.io/ballware/ballware-document-service` and
`ghcr.io/ballware/ballware-document-app`.

---

## 6. CI/CD

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `publish-packages.yml` | push to `main`, `develop` | Restore/build/pack with the GitVersion semver, push NuGet packages to GitHub Packages, tag `v<semver>` on `main`, build and push both Docker images for `linux/amd64` + `linux/arm64` with tags `<semver>` and `stable` (`main`) or `latest` (`develop`) |
| `sonarqube-latest.yml` | push to `main`, `develop`, `feature/*`, `bugfix/*` | `dotnet-sonarscanner` on project key `ballware_ballware-document-service`, Release build, all tests with `XPlat Code Coverage` (OpenCover) uploaded as coverage, `**/Migrations/**` excluded |

Required repository secrets: `DEVEXPRESS_NUGETFEED`, `DEVEXPRESS_LICENSE`, `SONAR_TOKEN`,
`TEST_DOCUMENT_PASSWORD` (`GITHUB_TOKEN` is provided by Actions).

Versioning follows `GitVersion.yml` in `ContinuousDeployment` mode: `main` increments patch,
`develop`/`feature/*` increment minor, `bugfix/*` increments patch, and commit messages can carry
`+semver: major|minor|patch`. Branch flow implies pre-release suffixes; only `main` publishes the
`stable` image tag and version tags.

---

## 7. Tests

```bash
dotnet test ballware-document-service.sln -c Release
```

| Suite | Coverage |
| --- | --- |
| `test/Ballware.Document.Api.Tests` | endpoint behaviour for document, notification, subscription and processing-state APIs (in-memory `TestHost`, Moq) |
| `test/Ballware.Document.Data.Ef.Postgres.Tests` | EF repositories against PostgreSQL (`Storage:AutoMigrations = true`) |
| `test/Ballware.Document.Data.Ef.SqlServer.Tests` | EF repositories against SQL Server |

Database-backed suites read credentials from configuration, e.g. the `TestSettings` section or
the `TestSettings__DatabasePassword` environment variable used by CI.

---

## 8. Security notes

- Client-credentials secrets belong in `appsettings.local.json`, environment variables or
  secrets managers — never in committed files. `appsettings.local.json` is untracked by design.
- Client certificates of the platform HTTP clients are only accepted unconditionally in `DEBUG`
  builds (`#if DEBUG` around `DangerousAcceptAnyServerCertificateValidator`); Release builds
  validate them.
- `/signon` rejects absolute or foreign-host `redirect` values and paths outside
  `Authorization:SignOnAllowedRedirectPathPrefixes`, and the App host distinguishes browser
  requests (OIDC 302 challenge) from SignalR/DevExpress transport requests (`/_blazor`, `/DXX`,
  `/dxDR`), which receive `401` so the frontend can re-enter via `/signon` instead of failing
  inside a circuit.
- Identity-model PII disclosure (`IdentityModelEventSource.ShowPII`) is enabled only in Development;
  production errors are returned as RFC 7807 `application/problem+json` without exception details.

---

## 9. License

MIT — see [LICENSE](LICENSE). © 2025 ballware Software & Consulting.
