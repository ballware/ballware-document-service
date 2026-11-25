# Service API

The [`service` OpenAPI document](../openapi/service.json) — 9 operations, title *ballware
document service API*, `v1`. These operations are for service-to-service calls: no user claims
are read; every operation carries explicit `{tenantId}` (and where relevant `{userId}`) path
segments and requires the **`serviceApi` policy** (token with `serviceApi` scope, obtained via
client credentials — see [README.md](README.md#authentication)).

No caller inside this repository consumes them over HTTP (in-process code uses the repositories
directly); they exist so platform services holding the machine scope can read/write document,
notification and subscription records for a tenant.

## Document

| Method | Path | Request body | Success response | Behavior |
| --- | --- | --- | --- | --- |
| GET | `/document/document/selectlistdocumentsfortenant/{tenantId}` | – | `200`, array of `DocumentSelectListEntry` | unfiltered select list for the tenant (no `visiblestate` filtering — that is a user-context feature) |
| GET | `/document/document/documentmetadatabytenantandid/{tenantId}/{id}` | – | `200`, `Document`; `404` | full metadata row (no `ReportBinary` hydration — that needs the user-API `byid?identifier=exportjson` hook) |
| GET | `/document/document/documenttemplatebehalfofuserbytenant/{tenantId}` | – | `200`, `Document` | new document template, saved-identity pre-filled (`NewAsync(tenantId, "primary", …)`, `DocumentMetaEndpoint.cs:177-180`) |
| POST | `/document/document/savedocumentbehalfofuser/{tenantId}/{userId}` | `Document` JSON | `200` empty | saves behalf of `{userId}` with identifier `primary`, no rights scripts (`DocumentMetaEndpoint.cs:182-187`) |

## Notification

| Method | Path | Request body | Success response |
| --- | --- | --- | --- |
| GET | `/document/notification/notificationmetadatabytenantandid/{tenantId}/{id}` | – | `200`, `Notification`; `404` |
| GET | `/document/notification/notificationmetadatabytenantandidentifier/{tenantId}/{identifier}` | – | `200`, `Notification`; `404` — looks the notification up by its `Identifier` field |

## Subscription

| Method | Path | Request body | Success response | Behavior |
| --- | --- | --- | --- | --- |
| GET | `/document/subscription/activebytenantandfrequency/{tenantId}/{frequency}` | – | `200`, array of `Subscription` | subscriptions with `Active = true` and `Frequency = {frequency}` |
| GET | `/document/subscription/metadatabytenantandid/{tenantId}/{id}` | – | `200`, `Subscription`; `404` | |
| POST | `/document/subscription/setsendresult/{tenantId}/{id}` | raw JSON **string** body | `200` empty | the string is written to `Subscription.LastError` together with a fresh `LastSendStamp` (`SubscriptionMetaEndpoint.cs:140-146` → `SubscriptionBaseRepository.SetLastErrorAsync`) |

`setsendresult` is the external counterpart of the in-process delivery write-back: an external
delivery orchestrator can fetch `activebytenantandfrequency`, process subscriptions, and report
the per-subscription outcome (`"OK"` or an error message) back to the record
(see [concepts/jobs-and-delivery.md](../concepts/jobs-and-delivery.md)).

## Examples

```bash
# machine token (your IdP's client-credentials flow)
curl -s "http://localhost:5004/document/document/selectlistdocumentsfortenant/<tenantGuid>" \
  -H "Authorization: Bearer $SERVICE_TOKEN"

# create a document on behalf of a user
curl -s -X POST "http://localhost:5004/document/document/savedocumentbehalfofuser/<tenantGuid>/<userGuid>" \
  -H "Authorization: Bearer $SERVICE_TOKEN" -H "Content-Type: application/json" \
  -d '{ "Id": "<guid>", "DisplayName": "<name>", "Entity": "<entity>", "State": <n> }'

# report a failed delivery
curl -s -X POST "http://localhost:5004/document/subscription/setsendresult/<tenantGuid>/<subscriptionGuid>" \
  -H "Authorization: Bearer $SERVICE_TOKEN" -H "Content-Type: application/json" \
  -d '"SMTP connection refused"'
```

## Status codes

`401` (missing token or `serviceApi` scope), `404` (record not found), `500` as
`application/problem+json`.
