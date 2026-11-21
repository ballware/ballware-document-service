# Generic CRUD operations

All three resources of this service (`document`, `notification`, `subscription`) are mounted
with the same 11 generic editing operations from
`ballware-shared-dotnet/src/Ballware.Shared.Api.Endpoints/TenantantableEditingEndpoint.cs`
(handler implementation in `TenantableEndpointHandlerFactory.cs`), mounted three times in
`src/Ballware.Document.Service/Startup.cs:220,224,228` with `application="meta"`,
`apiGroup="document"`.

All 11 operations require the **`documentApi` endpoint policy**. The policy only checks the
scope — the handlers then compute a **right candidate** and let the tenant's rights scripts
decide (see [concepts/tenancy-and-authorization.md](../concepts/tenancy-and-authorization.md)):
`all`/`query`/`byid` use `view`, `new` uses `add`, `remove/{id}` uses `delete`, `save`/`savebatch`
use `edit` unless the `identifier` query parameter is present and not `primary` — then the
identifier itself is the right. `export`/`exporturl` always use the identifier as the right.
`import` only requires the document scope at request time; the scheduled import job later checks
`meta.<entity>.<identifier>` per item.

## Operation overview

`<entity>` ∈ {`document`, `notification`, `subscription`}:

| # | Method | Path | Parameters | Request body | Success response | Right candidate |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | GET | `/document/<entity>/all` | `identifier` (query) | – | `200`, array of `<entity>` schema | `meta.<entity>.view` |
| 2 | GET | `/document/<entity>/query` | `identifier` (query) + free query params | – | `200`, array of `<entity>` schema | `meta.<entity>.view` |
| 3 | GET | `/document/<entity>/new` | `identifier` (query) | – | `200`, `<entity>` schema | `meta.<entity>.add` |
| 4 | GET | `/document/<entity>/byid` | `identifier`, `id` (both query; `id` is uuid) | – | `200`, `<entity>` schema; `404` if not found | `meta.<entity>.view` |
| 5 | POST | `/document/<entity>/save` | `identifier` (query) | `<entity>` JSON | `200` empty | `meta.<entity>.edit` or `meta.<entity>.<identifier>` |
| 6 | POST | `/document/<entity>/savebatch` | `identifier` (query) | array of `<entity>` JSON | `200` empty | `meta.<entity>.edit` or `meta.<entity>.<identifier>` |
| 7 | DELETE | `/document/<entity>/remove/{id}` | `id` (path, uuid) | – | `200` empty; `400` if remove precondition failed | `meta.<entity>.delete` |
| 8 | POST | `/document/<entity>/import` | `identifier` (query) | `multipart/form-data`, field `files` (file array) | `201` empty; schedules a job | document scope only (job checks `meta.<entity>.<identifier>`) |
| 9 | GET | `/document/<entity>/export` | `identifier` (query) + query params | – | `200`, raw export body | `meta.<entity>.<identifier>` |
| 10 | POST | `/document/<entity>/exporturl` | `identifier` (query) | `application/x-www-form-urlencoded` fields | `200`, `ExportUrlResult` | `meta.<entity>.<identifier>` |
| 11 | GET | `/document/<entity>/download/{tenantId}/{id}` | `tenantId`, `id` (path, uuid) | – | `200`, file content | **anonymous** |

> The snapshots mark `identifier` as `required: true`; at runtime the handlers treat it as the
> query-definition selector with the platform convention `primary` (see below) — always send it
> explicitly.

## Query and by-id semantics

- The `identifier` selects which embedded query definition of the tenant's entity metadata runs;
  the conventional default is `primary`. Other conventions seen on the platform: `exportjson`,
  `importjson`, `state` — all of them are tenant metadata maintained in the Meta Service, not
  hardcoded here.
- Extra query parameters on `query` are passed to the query definition as parameters. A parameter
  repeated multiple times arrives merged as `|value1|value2|`
  (`TenantableEndpointHandlerFactory.cs` `GetQueryParams`):

  ```text
  GET /document/document/query?identifier=primary&Entity=<entity>&State=1&State=2  →  State = "|1|2|"
  ```

- `byid` takes `identifier` **and** `id` as query parameters:
  `GET /document/document/byid?identifier=primary&id=<guid>` — `identifier` selects the by-id
  query definition entry; the `Document` import/export hook additionally hydrates `ReportBinary`
  from the storage attachment when `identifier=exportjson`
  (`src/Ballware.Document.Service/Extensions/DocumentImportExportRepositoryHook.cs`).

## Save

`save` posts a single document; `savebatch` posts an array and is checked item by item.
Responses are empty `200` — re-read via `byid` to get the stored document.

```bash
curl -s -X POST "http://localhost:5004/document/document/save?identifier=primary" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "Id": "<guid>", "DisplayName": "<name>", "Entity": "<entity>", "State": <n>, "ReportParameter": null }'
```

A non-`primary` identifier (e.g. `?identifier=state` for a state transition) requires the right
`meta.<entity>.<identifier>` and runs the matching entry of the entity's save statements —
the rights script decides whether the requested state transition is allowed.

## Remove

`remove/{id}` loads the item with the `primary` by-id query, checks `meta.<entity>.delete`, then
runs the entity's remove precondition scripts. If the precondition check reports failures, the
response is `400` with the joined messages as body. Removing a `document` also drops its
`report.xml` storage attachment (`DocumentImportExportRepositoryHook.BeforeRemove`).

## Export → exporturl → download

`export` returns the export payload inline; `exporturl` persists it as an `Export` record (via
the Meta Service) plus a temporary file (via the Storage Service) and returns an anonymous
download reference valid for **one day** (`TenantableEndpointHandlerFactory.cs`,
`ExpirationStamp = DateTime.Now.AddDays(1)`):

```text
GET  /document/<entity>/export?identifier=exportjson&<query params>   raw body, media type from the export definition
POST /document/<entity>/exporturl?identifier=exportjson               form fields → query params → ExportUrlResult {TenantId, Id}
     form body: param1=value1&param2=value2                          Export record expires = now + 1 day
GET  /document/<entity>/download/<tenantId>/<id>                     anonymous; file named <Query>_<timestamp><ext>
                                                                    404 when Export not found or expired
```

Payload/response details:

- `export` responses are declared `application/json` with a string schema in the snapshot
  (`Produces<string>()`); at runtime the content type is the export definition's media type and
  the body is the raw export data.
- `exporturl` accepts form fields — every field becomes a query parameter of the export query
  definition.
- `ExportUrlResult` = `{ "TenantId": "<guid>", "Id": "<guid>" }`.

## Import

`import` uploads one or more files (form field `files`) and returns `201 Created` immediately.
The request only needs the document scope. The handler uploads each file to temporary storage,
creates a job record in the Meta Service and schedules the durable `import` job of the entity
group; the job imports the payload per item and checks `meta.<entity>.<identifier>`
(see [concepts/jobs-and-delivery.md](../concepts/jobs-and-delivery.md#import-chain)).

```bash
curl -s -X POST "http://localhost:5004/document/document/import?identifier=importjson" \
  -H "Authorization: Bearer $TOKEN" \
  -F "files=@<document>.json"
```

For `document` with `identifier=importjson`, the repository hook stores the payload's
`ReportBinary` as the document's `report.xml` attachment before save.

## Status codes

`401` (missing scope / denied right), `404` (tenant, entity metadata or item not found), `400`
(remove preconditions), `500` as `application/problem+json` — see
[concepts/tenancy-and-authorization.md](../concepts/tenancy-and-authorization.md#status-code-matrix).
