# Architecture

## Hosts

The repository builds two deployable ASP.NET Core hosts from one shared code base
(repository [README.md](../../README.md) §1):

| Host | Role | Auth | Default dev port |
| --- | --- | --- | --- |
| `Ballware.Document.Service` | Headless REST API (both OpenAPI documents) + Quartz scheduler + DevExpress reporting core | JWT bearer only | `5004` |
| `Ballware.Document.App` | Blazor Web App with `DxReportDesigner` / `DxDocumentViewer` | Session cookie via OIDC sign-on (+ bearer for direct calls) | `5005` |

Neither host references the other; both share `Ballware.Document.Hosting` (options binding,
authentication setup, service clients, datastore selection, metadata providers, Mapster
profiles) and the same EF Core data store. The Service host registers the **reporting core**
(rendering, datasources, mail) via `AddBallwareDevExpressReportingCore()`
(`src/Ballware.Document.Service/Startup.cs:122`); the App host additionally registers the
Blazor reporting stack (`AddBallwareDevExpressReportingBlazor()`).

All OpenAPI documents described in [../api/README.md](../api/README.md) belong to the
**Service host**.

## Project responsibilities

| Project | Responsibility |
| --- | --- |
| `Ballware.Document.Service` | API host composition (`Program.cs`, `Startup.cs`), Swagger documents, Quartz host, import/export repository hook |
| `Ballware.Document.App` | Blazor Web App composition, `/signon` endpoint, challenge-differentiation middleware |
| `Ballware.Document.Api` | Minimal-API endpoint groups: document / notification / subscription / processing-state |
| `Ballware.Document.Hosting` | Options + validation, authentication and policies, service clients, datastore selection, metadata provider adapters, sign-on endpoint |
| `Ballware.Document.Engine.Dx` | DevExpress reporting integration, report storage, datasource provider, mail/PDF generation, designer/viewer components |
| `Ballware.Document.Jobs` | Quartz job and trigger registration (`MailOptions`, `TriggerOptions`) |
| `Ballware.Document.Data` | Public contracts (`Document`, `Notification`, `Subscription`), repository interfaces, select-list DTOs |
| `Ballware.Document.Data.Ef[.Postgres/.SqlServer]` | DbContext, model customization, EF repositories, provider migrations |
| `Ballware.Document.Metadata` | Provider contracts resolved by `Hosting.Adapter` implementations against Meta/Storage/Generic/ML clients |

## Mounted routes (Service host)

`src/Ballware.Document.Service/Startup.cs:218-230`:

```text
app.MapDocumentUserApi("/document/document");
app.MapDocumentServiceApi("/document/document");
app.MapTenantableEditingApi<Document>("/document/document", "meta", "document", …, "documentApi", "document");

app.MapNotificationUserApi("/document/notification");
app.MapNotificationServiceApi("/document/notification");
app.MapTenantableEditingApi<Notification>("/document/notification", "meta", "notification", …, "documentApi", "document");

app.MapSubscriptionUserApi("/document/subscription");
app.MapSubscriptionServiceApi("/document/subscription");
app.MapTenantableEditingApi<Subscription>("/document/subscription", "meta", "subscription", …, "documentApi", "document");

app.MapProcessingStateDocumentApi("/document/processingstate");
```

- The first parameter after the path (`"meta"`) is the **application**, the second the
  **entity** — together they address the entity metadata in the Meta Service and the right
  candidates `meta.<entity>.<right>` (see
  [tenancy-and-authorization.md](tenancy-and-authorization.md)).
- `apiGroup` is `"document"` for every user-facing mount and `"service"` for the service
  mounts, which is what routes all operations into exactly the two Swagger documents
  (`Startup.cs:128-138`). Tags (`Document`, `Notification`, `Subscription`, `ProcessingState`)
  drive the UI categories, groups drive the OpenAPI document.
- The generic editing mounts come from
  `ballware-shared-dotnet/src/Ballware.Shared.Api.Endpoints/TenantantableEditingEndpoint.cs`
  and contribute the 11 CRUD operations per entity ([../api/generic-crud.md](../api/generic-crud.md)).

## Persistence

`Storage:Provider` selects `postgres` or `mssql` at startup
(`Ballware.Document.Hosting/ServiceCollectionExtensions.cs:86-91,249-256`); both provider
assemblies ship in the solution with their own migration sets and consistent snake_case naming.
Migrations run in the Service host only when `Storage:AutoMigrations` is `true`. Repositories
are tenant-scoped everywhere.

## Error contract

Unhandled exceptions are returned as RFC 7807 `application/problem+json` with `500`
(`Startup.cs:167-192`); exception details are included only in Development. Endpoint handlers
themselves return plain `401` / `404` / `400` results (see
[tenancy-and-authorization.md](tenancy-and-authorization.md) for the matrix).

## Request size limits

Report payloads are large, so both hosts raise Kestrel limits
(`src/Ballware.Document.Service/appsettings.json`): `MaxRequestBodySize`,
`MaxRequestBufferSize` and `MaxRequestHeadersTotalSize` = 40 000 000 bytes,
`MaxRequestLineSize` = 24 576. The Service host also raises the form value length limit to
`int.MaxValue` (`Startup.cs:65-68`) for multipart imports.

## Culture

The request culture is pinned to `de-DE` for every request (`Startup.cs:207-212`); DevExpress
localization bundles for the JS controls ship as embedded static assets in `Engine.Dx`.
