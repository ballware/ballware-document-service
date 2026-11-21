# Document model

Public contracts: `src/Ballware.Document.Data/Public/*.cs`. The same shapes are exposed as
OpenAPI component schemas — see [../api/schemas.md](../api/schemas.md).

## `Document`

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | uuid | stable identity across export/import |
| `DisplayName` | string | human-readable name |
| `Entity` | string | the business entity the report is bound to (e.g. used by `selectlistdocumentsforentity/{entity}`) |
| `State` | int32 | processing-state number; state names live in the tenant's Meta Service metadata |
| `ReportBinary` | byte[] | serialized DevExpress XtraReport definition |
| `ReportParameter` | string | embedded JSON string: parameter values carried with the document |

## `ReportBinary` — two encodings

`ReportBinary` is a `byte[]`. Encodings differ by transport:

- **API/JSON transport:** JSON encodes `byte[]` as **base64** (`Document` schema:
  `type: string, format: byte`).
- **Export/import payload convention:** export and import query definitions carry
  `ReportBinary` as a **hex string**, and the same hex string is fed back unchanged through
  `identifier=importjson`.

Storage side (`src/Ballware.Document.Service/Extensions/DocumentImportExportRepositoryHook.cs`):

- `ExtendById` with identifier `exportjson`: downloads the attachment `report.xml` from the
  Storage Service and fills `ReportBinary`, so an export payload can carry the report definition
  inline.
- `BeforeSave` with identifier `importjson`: uploads `ReportBinary` as attachment `report.xml`
  on behalf of the importing user.
- `BeforeRemove`: drops the `report.xml` attachment.

So EF stores only the metadata row; the heavy report XML lives in the Storage Service as a
file attachment.

## `Notification`

Delivery definition:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | uuid | |
| `Identifier` | string | tenant-local identifier |
| `Name` | string | subject shown in the mail |
| `DocumentId` | uuid? | the document to render; delivery fails when null |
| `State` | int32 | processing state of the notification itself |
| `DocumentParams` | string | embedded JSON string, deserialized as a list of report parameters (`[{"Name":"<name>","Value":"<value>"}]`) before rendering (`src/Ballware.Document.Jobs/Internal/SubscriptionTriggerJob.cs:62-66`) |

## `Subscription`

Delivery schedule for one user:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | uuid | |
| `UserId` | uuid | recipient user |
| `Mail` | string | recipient e-mail; delivery only runs when set |
| `Body` | string | mail body (used for the attachment variant) |
| `Attachment` | bool | render the document as PDF attachment vs. inline mail body |
| `AttachmentFileName` | string | attachment file name |
| `NotificationId` | uuid | the `Notification` (delivery definition) to use |
| `Frequency` | int32 | frequency bucket matched against `Trigger:Active[].Frequency` cron triggers |
| `Active` | bool | inactive subscriptions are skipped by the scheduler and the select lists expose the flag |
| `LastSendStamp` | date-time? | written by `SetLastErrorAsync` on every send attempt (`src/Ballware.Document.Data.Ef/Repository/SubscriptionBaseRepository.cs:33-43`) |
| `LastError` | string? | `"OK"` on success, otherwise the exception message; also written by the Service API `setsendresult` |

## Select-list DTOs

Lightweight shapes used by `selectlist` / `selectbyid` endpoints
(`src/Ballware.Document.Data/SelectLists/`):

| DTO | Fields |
| --- | --- |
| `DocumentSelectListEntry` | `Id`, `Name` (from `DisplayName`), `State`, `ReportParameter` |
| `NotificationSelectListEntry` | `Id`, `Name` |
| `SubscriptionSelectListEntry` | `Id`, `UserId`, `NotificationId`, `Mail`, `Active` |

These are dropdown/UI-oriented projections — full payloads come from the generic CRUD
operations ([../api/generic-crud.md](../api/generic-crud.md)).

## Processing states

State semantics are tenant metadata in the Meta Service (`meta.<entity>.<identifier>` query
definitions, `visiblestate.<state>` rights). The service exposes them read-only through
`ProcessingStateSelectListEntry`:

| Field | Type | Meaning |
| --- | --- | --- |
| `Id` | uuid | processing-state metadata id |
| `Name` | string | localized state name |
| `State` | int32 | state number stored in `Document.State` / `Notification.State` |
| `Locked` | bool | state must not be edited |
| `Finished` | bool | terminal workflow state |
| `ReasonRequired` | bool | a change reason is expected when moving into this state |

`selectlistallowedsuccessorsforentities/document|notification` returns the intersection of the
allowed successor states of **all** supplied `id` values: per id, the current state's
successors are filtered by `visiblestate.<state>` rights and then intersected across ids
(`ProcessingStateMetaEndpoint.cs:72-86,116-130`) — the result is what a bulk transition may
target.
