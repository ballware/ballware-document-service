# docs/ folder for ballware-document-service (API + payloads)

## 1. Goal

Add a repository `docs/` folder that explains the service's API surface and data payloads in detail,
derived from committed OpenAPI snapshots, laid out like `ballware-meta-service/docs` so that
`ballware-docs` can consume it later. Setup/configuration stays in `README.md`; `docs/` covers
functionality + API contract.

## 2. Agreed decisions

| Topic | Decision |
| --- | --- |
| Hidden user operations | **Fix the Swashbuckle group names** so all user operations appear in the `document` OpenAPI document (source change, WP1) |
| Snapshot generation | Offline `swagger tofile` (`ASPNETCORE_ENVIRONMENT=Swagger`) + committed snapshots + `manifest.json`; live-server export documented as fallback |
| Structure | Meta-parity: `docs/concepts/`, `docs/api/`, `docs/openapi/` + `docs/api/best-practices.md` |
| Language / production data | English; platform conventions (`primary`, `exportjson`, `importjson`, `state`, `meta.<entity>.<right>`, `visiblestate.<state>`) documented, but no GUIDs, tenant names, production states/rights/scripts, no file copies from `ballware-fwpro` |

## 3. Findings that shape the work (evidence)

- Only two Swagger documents are declared: `document`, `service`
  (`src/Ballware.Document.Service/Startup.cs:128-138`, UI wiring `:241-253`).
- Six user operations are mounted with group names that have no Swagger document and are therefore
  missing from every published spec (confirmed against
  `ballware-docs/openapi/reporting/document.json`, 38 paths):

  | Operation | Current group | Defined at |
  | --- | --- | --- |
  | `GET /document/notification/selectlist`, `/selectbyid/{id}` | `notification` | `NotificationMetaEndpoint.cs:24,31,40` |
  | `GET /document/subscription/selectlist`, `/selectbyid/{id}`, `POST /trigger` | `subscription` | `SubscriptionMetaEndpoint.cs:25,32,41,51` |
  | `GET /document/processingstate/selectlistallowedsuccessorsforentities/notification` | `notification` | `ProcessingStateMetaEndpoint.cs:37` |

- Generic CRUD is mounted 3× with `application="meta"`, group `document`
  (`Startup.cs:220,224,228`) → the 11 operations from
  `ballware-shared-dotnet/src/Ballware.Shared.Api.Endpoints/TenantantableEditingEndpoint.cs`
  (`all, query, new, byid, save, savebatch, remove/{id}, import, export, exporturl, download/{tenantId}/{id}`;
  `download` is anonymous).
- JSON bodies are PascalCase (`Startup.cs:85-93`); security scheme `oidc` with
  `openIdConnectUrl = Authority + /.well-known/openid-configuration` (`Startup.cs:142-146`) → real IdP
  URL leaks into a snapshot if the Authority is not pinned.
- Payload shapes verified in the current aggregator snapshot:
  - `document` doc: 38 paths, schemas `Document`, `DocumentSelectListEntry`, `Notification`,
    `Subscription`, `ProcessingStateSelectListEntry`, `ExportUrlResult`, `StringStringValuesKeyValuePair`.
  - `service` doc: 9 paths, 4 schemas.
  - `byid` takes `identifier` **and** `id` as query params; `export` returns a raw string body;
    `exporturl` accepts `application/x-www-form-urlencoded` and returns `ExportUrlResult {TenantId, Id}`.
  - `updatedatasources` has **no** documented request body — ids are repeated `id` **query** params
    (`DocumentMetaEndpoint.cs:189-212`, `QueryValueBag.BindAsync`); same for `subscription/trigger`
    (`SubscriptionMetaEndpoint.cs:148-171`).
- Mandatory config sections that any Swagger environment must satisfy, otherwise
  `Environment.Exit(-1)` (`Program.cs:33-37`): `Authorization` (`ServiceCollectionExtensions.cs:79`),
  `Storage` + `ConnectionStrings` entry (`:90,239,246`), `MetaClient`/`StorageClient`/`GenericClient`/
  `MlClient` (`:100,105,110,115`), `Mail` (`Startup.cs:55-58`).
- Offline spec generation risk: the Service host registers DevExpress reporting
  (`Startup.cs:122`) and executes DevExpress static registration during app configuration
  (`Engine.Dx/ApplicationBuilderExtensions.cs:11-26`), so `swagger tofile` may need the private NuGet
  feed + DevExpress license; `appsettings.Swagger.json` does not exist in this repo yet.
- Tool pin: Swashbuckle 10.3.0 (`Ballware.Document.Service.csproj:20-21`, CI pin
  `publish-packages.yml:34`) → `Swashbuckle.AspNetCore.Cli 10.3.0` (meta docs pin 10.1.3, do not copy).

## 4. Tasks

### WP1 — Expose all user operations in the `document` document (source fix)

1. `src/Ballware.Document.Api/Endpoints/NotificationMetaEndpoint.cs:24` — default `apiGroup`
   `"notification"` → `"document"`.
2. `src/Ballware.Document.Api/Endpoints/SubscriptionMetaEndpoint.cs:25` — default `apiGroup`
   `"subscription"` → `"document"`.
3. `src/Ballware.Document.Api/Endpoints/ProcessingStateMetaEndpoint.cs` — add trailing parameter
   `string apiGroup = "document"` and replace the hardcoded `.WithGroupName("document")` (line 28) and
   `.WithGroupName("notification")` (line 37) with `.WithGroupName(apiGroup)`.
4. Keep `WithTags(...)` unchanged — tags drive the Swagger UI / ballware-docs categories.
5. Keep `Startup.cs:218-230` mount calls unchanged (defaults carry the fix).
6. Confirm no external callers of these `Map*UserApi` methods exist (checked: only `Startup.cs` and
   `test/Ballware.Document.Api.Tests/*`), because adding an optional parameter to a published
   package method is source-compatible but not binary-compatible for precompiled callers.

Expected result: `document` document grows from 38 → 44 paths; `service` stays at 9. Operation ids
(`NotificationSelectList`, `SubscriptionTriggerByIds`,
`ProcessingStateSelectListAllSuccessorsForNotificationByIds`, …) are unique, no collisions.

### WP2 — Swagger environment + OpenAPI snapshots

1. New `src/Ballware.Document.Service/appsettings.Swagger.json` modeled on
   `ballware-meta-service/src/Ballware.Meta.Service/appsettings.Swagger.json`: fake `Authorization`
   (`Authority: "https://localhost"`, `Audience`, `ClientId`), fake `MetaClient`/`StorageClient`/
   `GenericClient`/`MlClient`, fake `Mail`, `Storage` (`AutoMigrations: false`) + a
   `ConnectionStrings` entry, `Swagger` (`EnableClient: true`, `ClientId: "fake"`,
   `RequiredScopes: "openid documentApi serviceApi"`), Quartz `quartz.jobStore.type:
   "Quartz.Simpl.RAMJobStore, Quartz"`.
2. New `docs/openapi/fetch-spec.sh` adapted from the meta-service script:
   `TOOL_VERSION=10.3.0`, `CONFIG_PROJECT=src/Ballware.Document.Service`,
   assembly `bin/Release/net10.0/Ballware.Document.Service.dll`, documents `document` and `service`,
   output `docs/openapi/{document,service}.json`, `export ASPNETCORE_ENVIRONMENT=Swagger` and
   `export Authorization__Authority="https://localhost"` (pinned so a developer
   `appsettings.local.json` cannot bake the real IdP URL into the commit), validate each file
   (JSON, `openapi` starts with `3.`, `paths` non-empty), write `manifest.json` with
   `source/toolVersion/environment/serviceVersion (git describe)/fetchedAt/per-document
   openapi+title+pathCount+schemaCount`.
3. First run is a **verification gate**: if `swagger tofile` cannot resolve/initialize the host
   (DevExpress assembly/license load), switch the script to the live export fallback
   (`ASPNETCORE_ENVIRONMENT=Swagger dotnet run --project src/Ballware.Document.Service` +
   `curl http://localhost:5004/swagger/{document,service}/swagger.json`) and state the reason in
   `docs/openapi/README.md`. Do not weaken production code to make offline export work.
4. Commit `docs/openapi/document.json`, `docs/openapi/service.json`, `docs/openapi/manifest.json`
   (generated in this run; record the actual OpenAPI version instead of assuming 3.0.1 or 3.0.4).

### WP3 — `docs/concepts/` (how the service works)

- `docs/README.md` — index table of all pages, quick facts (two OpenAPI documents, titles
  "ballware document user API" / "ballware document service API", `v1`, ports 5004/5005,
  JWT bearer + scope→policy `documentApi`/`serviceApi`, PascalCase JSON), request-flow diagram
  (token → routing → JWT → policy → handler → rights scripts → repository/EF → meta/storage/generic/ml
  clients), scope note pointing to `README.md` for setup and to `ballware-angular` docs for the
  frontend metadata contract.
- `concepts/architecture.md` — hosts (Service = JWT-only API + Quartz, App = Blazor designer/viewer),
  project responsibilities, mounted routes (`Startup.cs:218-230`), provider-independent persistence,
  error contract `application/problem+json` (`Startup.cs:167-192`), request size limits, culture `de-DE`.
- `concepts/tenancy-and-authorization.md` — claims (`tenant`, `sub`, `right`), policies and required
  scopes, right candidates resolved as `meta.<entity>.<right>` for every generic operation,
  `visiblestate.<state>` filtering in `selectlistdocumentsforentity` (`DocumentMetaEndpoint.cs:129-136`),
  status-code matrix, sign-on/session flow of the App host (`/signon/{idToken}`, `redirect` validation)
  and the browser-vs-transport distinction (`/_blazor`, `/DXX`, `/dxDR` → 401).
- `concepts/document-model.md` — `Document` / `Notification` / `Subscription` fields
  (`src/Ballware.Document.Data/Public/*.cs`), `ReportBinary` (`byte[]` → base64 in JSON) vs. the
  hex-string convention used by export/import query definitions, `ReportParameter` / `DocumentParams`
  as embedded JSON strings, select-list DTO fields
  (`DocumentSelectListEntry {Id, Name, State, ReportParameter}`, `NotificationSelectListEntry {Id, Name}`,
  `SubscriptionSelectListEntry {Id, UserId, NotificationId, Mail, Active}`), processing-state model
  (`ProcessingStateSelectListEntry {Id, Name, State, Locked, Finished, ReasonRequired}`, successor
  intersection across multiple ids).
- `concepts/datasources-and-designer.md` — `/designer?id=<documentId>` and `/viewer?<query>`,
  `ReportStorageWebExtension` (`DocumentStorage`), datasource providers (`MetaServiceProvider`,
  `GenericServiceProvider`, `MlServiceProvider` + Mapster profiles in `Hosting/Mappings/`), embedded
  report functions (`FetchNameByIdFromLookup`, `FetchTextByValueFromPickvalue`,
  `FetchNameByStateFromProcessingState`), note that the DevExpress viewer/designer transport endpoints
  are **not** part of the OpenAPI documents.
- `concepts/jobs-and-delivery.md` — Quartz clustered AdoJobStore, durable jobs
  (`import` per entity, `updatedatasources`, `trigger`, `SubscriptionFrequencyJob` cron from
  `Trigger:Active` with `frequency` job data), `JobDataMap` keys (`tenantId`, `userId`,
  `documentId` / `subscriptionId`), `Mail:AllowedTenants` gate, `LastSendStamp`/`LastError` write-back
  via `setsendresult`.

### WP4 — `docs/api/` (call and payload reference)

- `api/README.md` — the two documents (path/schema counts from the committed snapshots), group vs. tag
  semantics, `CustomSchemaIds`-free naming, content types, token acquisition (Swagger UI PKCE;
  client-credentials curl example for `serviceApi`), anonymous `download`, pointer to
  `openapi/README.md` for regeneration.
- `api/generic-crud.md` — the 11 operations applied to `document`, `notification`, `subscription`
  (method/path/params/body/response/right table), `identifier` semantics with `primary` as default,
  repeated query params merged to `|value1|value2|`, `save` vs. non-`primary` identifier rights,
  `remove/{id}` precondition `400`, import → job chain, export → exporturl → download lifecycle
  (`ExportUrlResult {TenantId, Id}`, 24 h validity, anonymous download, `404` after expiry).
- `api/document-api-resources.md` — user-specific operations with request/response detail:
  `selectlist`, `selectlistdocumentsforentity/{entity}` (state-rights filter), `selectbyid/{id}`,
  `POST updatedatasources` (repeated `id` query params, `201`, no body),
  `GET processingstate/selectlistallowedsuccessorsforentities/document|notification?
  id=<guid>&id=<guid>`, plus the now-exposed notification/subscription select operations and
  `POST subscription/trigger`.
- `api/service-api.md` — all 9 service operations with explicit `{tenantId}`/`{userId}` path
  segments, payload bodies (`savedocumentbehalfofuser` → `Document`, `setsendresult` → raw JSON string
  written to `LastError`), and which internal callers use them.
- `api/schemas.md` — field tables for every component schema of both snapshots
  (type/format/nullable), including `ExportUrlResult` and `StringStringValuesKeyValuePair`, marked as
  generated from the committed snapshots.
- `api/examples.md` — copy-pasteable curl sequences with `<placeholders>`: token fetch,
  select list, `byid?identifier=primary&id=<guid>`, `save?identifier=primary`,
  `save?identifier=state` (state transition), `export?identifier=exportjson&id=<guid>`,
  `exporturl` + anonymous `download`, `import?identifier=importjson` multipart round-trip,
  subscription trigger + frequency, service-API metadata + `setsendresult`.

### WP5 — `docs/api/best-practices.md`

Anonymized templates derived from `ballware-fwpro` **conventions only** (never literal file content):
query/save identifier set (`primary`, `exportjson`, `importjson`, `state`), the export/import payload
pair for a document (hex `ReportBinary` on the export side, same hex fed back through `importjson`),
the rights set expected for a document workflow (`view/add/edit/delete/exportjson/importjson/
visiblestate.<n>`), a subscription record template (`{Id, UserId, Mail, Body, Attachment,
AttachmentFileName, NotificationId, Frequency, Active}`), and a minimal notification + parameterized
`DocumentParams` template. Add an explicit note that identifiers/rights/states are tenant metadata
maintained in the Meta Service, not hardcoded by this service.

### WP6 — README wiring

Add a documentation index block to `README.md` (mirroring the meta-service README): links to
`docs/README.md`, `docs/api/README.md`, `docs/openapi/` (snapshots + regeneration). Note in the API
section that all `/document/**` user operations are contained in the `document` OpenAPI document.

## 5. Verification

1. `dotnet build ballware-document-service.sln -c Release` after WP1 (net10.0 + net8.0 targets).
2. `dotnet test test/Ballware.Document.Api.Tests -c Release` — endpoint tests must stay green; the DB
   suites are optional and may be skipped without a local Postgres/SQL Server.
3. Run `docs/openapi/fetch-spec.sh`; assert `document.json` has 44 paths including the six previously
   missing paths, `service.json` has 9 paths, both parse as OpenAPI `3.x`, and `manifest.json` records
   title/path/schema counts.
4. Confirm the generated `components.securitySchemes.oidc.openIdConnectUrl` points at
   `https://localhost/...` (fake), not the real IdP.
5. Link check inside `docs/` (every relative link resolves to an existing file) and check that no
   production GUIDs, tenant identifiers, state names, or rights-script text from `ballware-fwpro`
   appear (`rg` for the GUID prefixes seen in `meta/metadata-entities.json` must return nothing).
6. No new secrets committed; `appsettings.local.json` stays untracked and is not used as a source for
   the snapshots.

## 6. Risks / open points

- **Offline spec export may fail** because the host pulls DevExpress reporting into service/
  application registration; mitigation is the documented live-export fallback (WP2.3). Do not alter
  production wiring to work around it.
- `swagger tofile` output formatting depends on the pinned 10.3.0 tool; other versions create noise
  diffs (meta docs learned this with 10.1.3).
- WP1 changes the published API surface: `ballware-docs` regenerates 6 extra method pages on its next
  `api:fetch` + `api:gen`; its `openapi/reporting/document.json` snapshot is currently 38 paths.
- Adding an optional parameter to `MapProcessingStateDocumentApi` is source-compatible for all known
  in-repo callers only (Startup + tests).

## 7. Out of scope

- Changes in `ballware-docs` (sidebar wiring, `api:fetch`/`api:gen`, cross-repo doc ingestion).
- Any frontend (designer/viewer UI) documentation beyond the API contract.
- Copying or referencing concrete content of `ballware-fwpro/meta/metadata-entities.json` or
  `tenant/ffw_pr_oldendorf.json` in the docs.
