# Document API resources

Operations beyond the 11 generic CRUD ones, all in the
[`document` OpenAPI document](../openapi/document.json) (policy `documentApi`, tenant taken from
the `tenant` claim — no `{tenantId}` in these paths).

## Document

| Method | Path | Parameters | Success response | Notes |
| --- | --- | --- | --- | --- |
| GET | `/document/document/selectlist` | – | `200`, array of `DocumentSelectListEntry` | every document of the tenant, unfiltered |
| GET | `/document/document/selectlistdocumentsforentity/{entity}` | `entity` (path) | `200`, array of `DocumentSelectListEntry` | filtered per state: only documents whose `visiblestate.<State>` right is granted (`DocumentMetaEndpoint.cs:129-136`) |
| GET | `/document/document/selectbyid/{id}` | `id` (path, uuid) | `200`, `DocumentSelectListEntry`; `404` | single select-list entry |
| POST | `/document/document/updatedatasources` | `id` (query, repeated uuid) | `201` empty; `400` when no `id` given | **no request body** — ids are repeated query params; queues `updatedatasources` job per id (`DocumentMetaEndpoint.cs:189-212`); the snapshot's declared `404` never originates from this handler |

```text
POST /document/document/updatedatasources?id=<guid>&id=<guid>
```

Select-list entries carry `Id`, `Name`, `State`, `ReportParameter` — use them for pickers; use
`byid` (generic CRUD) for the full `Document` payload.

## Notification

| Method | Path | Parameters | Success response |
| --- | --- | --- | --- |
| GET | `/document/notification/selectlist` | – | `200`, array of `NotificationSelectListEntry` |
| GET | `/document/notification/selectbyid/{id}` | `id` (path, uuid) | `200`, `NotificationSelectListEntry`; `404` |

## Subscription

| Method | Path | Parameters | Success response | Notes |
| --- | --- | --- | --- | --- |
| GET | `/document/subscription/selectlist` | – | `200`, array of `SubscriptionSelectListEntry` | |
| GET | `/document/subscription/selectbyid/{id}` | `id` (path, uuid) | `200`, `SubscriptionSelectListEntry`; `404` | |
| POST | `/document/subscription/trigger` | `id` (query, repeated uuid) | `201` empty; `400` when no `id` given | **no request body** — schedules the `trigger` job per subscription id (`SubscriptionMetaEndpoint.cs:148-171`); unknown ids surface as job failures, not as a `404` from this endpoint (the snapshot declares `404` for consistency with the handler factory) |

```text
POST /document/subscription/trigger?id=<guid>&id=<guid>
```

Delivery itself (render + mail + `LastSendStamp`/`LastError` write-back) runs in
`SubscriptionTriggerJob` — see
[concepts/jobs-and-delivery.md](../concepts/jobs-and-delivery.md#delivery-chain-subscriptiontriggerjob).

## ProcessingState

| Method | Path | Parameters | Success response |
| --- | --- | --- | --- |
| GET | `/document/processingstate/selectlistallowedsuccessorsforentities/document` | `id` (query, repeated uuid) | `200`, array of `ProcessingStateSelectListEntry` |
| GET | `/document/processingstate/selectlistallowedsuccessorsforentities/notification` | `id` (query, repeated uuid) | `200`, array of `ProcessingStateSelectListEntry` |

Semantics (`ProcessingStateMetaEndpoint.cs:44-131`):

1. For each instance id: read its current `State`, resolve the entity's allowed successor
   processing states, drop successors whose `visiblestate.<state>` right is not granted.
2. When several ids are given, return the **intersection** across all ids — the states every
   listed instance may move to (bulk transition target set).
3. No ids (or unknown current states) → empty array. If the entity metadata is missing → `404`.

## Status codes

`401` missing/insufficient token, `404` entity/tenant/item metadata missing, `400` missing `id`
query params, `201` accepted-for-processing (`updatedatasources`, `trigger`); unhandled failures
as `500` `application/problem+json`.
