# Component schemas

Field tables for every component schema of the committed snapshots. **Generated from**
[openapi/document.json](../openapi/document.json) and [openapi/service.json](../openapi/service.json)
— regenerate both docs after `fetch-spec.sh` if a table drifts.

- Nullable `string?` properties appear as `type: string` with `nullable: true`.
- `Guid` → `type: string, format: uuid`; `int` → `integer, int32`; `DateTime?` → `string, format: date-time`.
- `byte[]` → `type: string, format: byte` (JSON base64; hex only in export/import payload conventions —
  see [concepts/document-model.md](../concepts/document-model.md#reportbinary--two-encodings)).
- `Document`, `DocumentSelectListEntry`, `Notification`, `Subscription` appear in **both** documents
  (each Swagger document is self-contained).

## `Document` (both documents)

| Field | Type | Nullable |
| --- | --- | --- |
| `Id` | string (uuid) | no |
| `DisplayName` | string | yes |
| `Entity` | string | yes |
| `State` | integer (int32) | no |
| `ReportBinary` | string (byte) | yes |
| `ReportParameter` | string | yes |

## `Notification` (both documents)

| Field | Type | Nullable |
| --- | --- | --- |
| `Id` | string (uuid) | no |
| `Identifier` | string | yes |
| `Name` | string | yes |
| `DocumentId` | string (uuid) | yes |
| `State` | integer (int32) | no |
| `DocumentParams` | string | yes |

## `Subscription` (both documents)

| Field | Type | Nullable |
| --- | --- | --- |
| `Id` | string (uuid) | no |
| `UserId` | string (uuid) | no |
| `Mail` | string | yes |
| `Body` | string | yes |
| `Attachment` | boolean | no |
| `AttachmentFileName` | string | yes |
| `NotificationId` | string (uuid) | no |
| `Frequency` | integer (int32) | no |
| `Active` | boolean | no |
| `LastSendStamp` | string (date-time) | yes |
| `LastError` | string | yes |

## `DocumentSelectListEntry` (both documents)

| Field | Type | Nullable |
| --- | --- | --- |
| `Id` | string (uuid) | no |
| `Name` | string | yes |
| `State` | integer (int32) | no |
| `ReportParameter` | string | yes |

## `NotificationSelectListEntry` (`document` only)

| Field | Type | Nullable |
| --- | --- | --- |
| `Id` | string (uuid) | no |
| `Name` | string | yes |

## `SubscriptionSelectListEntry` (`document` only)

| Field | Type | Nullable |
| --- | --- | --- |
| `Id` | string (uuid) | no |
| `UserId` | string (uuid) | no |
| `NotificationId` | string (uuid) | no |
| `Mail` | string | yes |
| `Active` | boolean | no |

## `ProcessingStateSelectListEntry` (`document` only)

| Field | Type | Nullable |
| --- | --- | --- |
| `Id` | string (uuid) | no |
| `Name` | string | yes |
| `State` | integer (int32) | no |
| `Locked` | boolean | no |
| `Finished` | boolean | no |
| `ReasonRequired` | boolean | no |

## `ExportUrlResult` (`document` only)

| Field | Type | Nullable |
| --- | --- | --- |
| `TenantId` | string (uuid) | no |
| `Id` | string (uuid) | no |

## `StringStringValuesKeyValuePair` (`document` only)

| Field | Type | Nullable |
| --- | --- | --- |
| `Key` | string | yes |
| `Value` | array of string | no |

The raw `export` response is **not** a component schema — the snapshot declares it as a plain
string (`Produces<string>()`); the actual media type and payload come from the export query
definition ([generic-crud.md](generic-crud.md#export--exporturl--download)).
