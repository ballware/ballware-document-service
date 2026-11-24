# Datasources and designer

The report designer/viewer runs in the **App host** (`Ballware.Document.App`, dev port
`5005`) — it is not part of the OpenAPI documents, but its behavior defines what the API
payloads must look like.

## Routes

| Route | Component | Purpose |
| --- | --- | --- |
| `/designer?id=<documentId>` | `Ballware.Document.Engine.Dx/Components/Designer.razor` | `DxReportDesigner` for editing a document's report definition |
| `/viewer?<report query>` | `Components/Viewer.razor` | `DxDocumentViewer` rendering the report (PDF/Skia) |
| `/signon/{idToken}` | `Ballware.Shared.Web.Oidc/SignOnEndpointExtensions.cs` | establishes the browser session (see [tenancy-and-authorization.md](tenancy-and-authorization.md)) |

Designer customization is client-side via `Engine.Dx/wwwroot/customization.js`; request culture
is `de-DE`.

## Report storage — `DocumentStorage`

`src/Ballware.Document.Engine.Dx/Internal/DocumentStorage.cs` is a DevExpress
`ReportStorageWebExtension` registered for the Blazor reporting stack:

- Report loading resolves the document id from the designer/viewer URL (document guid or print
  URL with instance ids) for the **current tenant** and returns the stored report layout — the
  report XML is read from the document's `report.xml` attachment via the Storage Service.
- Designer saves go back through the same tenant-scoped metadata path.
- Designer datasource editing (`AllowAdd/Edit/RemoveDataSource`) is enabled only when the user
  holds the document edit right (`meta.document.edit`, `Designer.razor:44`).
- `EnableCustomSql` is on (`Engine.Dx/ServiceCollectionExtensions.cs:26-29`), so reports may
  embed custom SQL queries against the injected datasources.

## Datasource providers

Report datasources are assembled per tenant from tenant metadata
(`Engine.Dx/Internal/DocumentDatasourceProvider.cs`):

- `IDatasourceDefinitionProvider` implementations are registered in
  `src/Ballware.Document.Hosting/ServiceCollectionExtensions.cs:282-284` and resolved from the
  platform services via `Hosting/Adapter/`:
  - `MetaServiceProvider` — entity datasources from the Meta Service (also processing states,
    pickvalues, authorization/job/export metadata),
  - `GenericServiceProvider` — lookups and generic datasource definitions from the Generic Service,
  - `MlServiceProvider` — ML datasource definitions from the ML Service.
- Definitions become DevExpress `SqlDataSource`s with provider-prefixed connection strings
  (`XpoProvider=Postgres;…` / `XpoProvider=MSSqlServer;…`).
- Payload mapping from the service clients into public models goes through Mapster profiles in
  `src/Ballware.Document.Hosting/Mappings/`
  (`MetaServiceDocumentMetadataProfile`, `GenericServiceDocumentMetadataProfile`,
  `MlServiceDocumentMetadataProfile`).

`POST /document/document/updatedatasources` (user API) refreshes a stored report's datasource
definitions so changed platform metadata is reflected without re-opening the designer — see
[jobs-and-delivery.md](jobs-and-delivery.md).

## Embedded report functions

Registered as custom criteria functions at app start
(`Engine.Dx/ApplicationBuilderExtensions.cs:19-26`), usable from report expressions and
filters:

| Function | Resolves |
| --- | --- |
| `FetchNameByIdFromLookup` | display name of a referenced entity instance from its id (Generic Service lookup) |
| `FetchTextByValueFromPickvalue` | label of a pickvalue item for `(entity, field, value)` (Meta Service) |
| `FetchNameByStateFromProcessingState` | localized processing-state name for `(entity, state)` (Meta Service) |

## Not part of the OpenAPI documents

The DevExpress viewer/designer transport endpoints (`/_blazor`, `/DXX`, `/dxDR`) are interactive
circuit endpoints of the App host, not REST operations — they appear in neither OpenAPI
document. They answer `401` (not a 302 challenge) when the session is gone, which is the signal
for the frontend to re-enter via `/signon`.
