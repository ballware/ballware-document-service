# Examples

Copy-pasteable sequences against the Service host (`http://localhost:5004` in development).
All ids, tenants and metadata names are `<placeholders>`; identifiers such as `primary`,
`exportjson`, `importjson`, `state` are platform conventions, resolved per tenant in the Meta
Service (see [best-practices.md](best-practices.md)).

## 1. Get a user token

Either via Swagger UI (PKCE sign-in) or directly from your IdP:

```bash
TOKEN=$(curl -s -X POST "<IdP-Base>/protocol/openid-connect/token" \
  -d "grant_type=client_credentials" -d "client_id=<client>" \
  -d "client_secret=<secret>" -d "scope=documentApi" | jq -r .access_token)
```

## 2. Select lists

```bash
curl -s "http://localhost:5004/document/document/selectlist" \
  -H "Authorization: Bearer $TOKEN"

curl -s "http://localhost:5004/document/document/selectlistdocumentsforentity/<entity>" \
  -H "Authorization: Bearer $TOKEN"   # visiblestate.<state>-filtered

curl -s "http://localhost:5004/document/notification/selectlist" -H "Authorization: Bearer $TOKEN"
curl -s "http://localhost:5004/document/subscription/selectlist" -H "Authorization: Bearer $TOKEN"
```

## 3. Read a document (full payload)

```bash
curl -s "http://localhost:5004/document/document/byid?identifier=primary&id=<guid>" \
  -H "Authorization: Bearer $TOKEN"
```

## 4. Save (new or update) and state transitions

```bash
curl -s -X POST "http://localhost:5004/document/document/save?identifier=primary" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "Id": "<guid>", "DisplayName": "<name>", "Entity": "<entity>", "State": <n>, "ReportParameter": null }'

# state transition: right candidate meta.document.state decides per transition
curl -s -X POST "http://localhost:5004/document/document/save?identifier=state" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "Id": "<guid>", "DisplayName": "<name>", "Entity": "<entity>", "State": <targetState> }'

# which target states are allowed for one/multiple documents?
curl -s "http://localhost:5004/document/processingstate/selectlistallowedsuccessorsforentities/document?id=<guid>&id=<guid>" \
  -H "Authorization: Bearer $TOKEN"
```

## 5. Export inline (`exportjson` convention)

```bash
curl -s "http://localhost:5004/document/document/export?identifier=exportjson&id=<guid>" \
  -H "Authorization: Bearer $TOKEN" -o document-export.json
```

Raw body with the export definition's media type. The payload carries `ReportBinary` as a hex
string (see [concepts/document-model.md](../concepts/document-model.md#reportbinary--two-encodings)).

## 6. Export to URL + anonymous download

```bash
curl -s -X POST "http://localhost:5004/document/document/exporturl?identifier=exportjson" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/x-www-form-urlencoded" \
  -d "id=<guid>"
# -> { "TenantId": "<tenantGuid>", "Id": "<exportGuid>" }

# anonymous, valid for one day:
curl -s -o export.json \
  "http://localhost:5004/document/document/download/<tenantGuid>/<exportGuid>"
```

Expired or unknown export id → `404`.

## 7. Import round-trip (`importjson` convention)

Feed the exported payload (same hex `ReportBinary`) back through import — `201` accepted, the
`import` job performs the actual import (per-item right check `meta.document.importjson`):

```bash
curl -s -X POST "http://localhost:5004/document/document/import?identifier=importjson" \
  -H "Authorization: Bearer $TOKEN" \
  -F "files=@document-export.json"
```

## 8. Refresh datasources

```bash
curl -s -X POST "http://localhost:5004/document/document/updatedatasources?id=<guid>&id=<guid>" \
  -H "Authorization: Bearer $TOKEN" -i   # 201; job queue, no body
```

## 9. Trigger a subscription and schedule it

```bash
curl -s -X POST "http://localhost:5004/document/subscription/trigger?id=<subscriptionGuid>" \
  -H "Authorization: Bearer $TOKEN" -i   # 201

# recurring: subscription.Frequency must match a Trigger:Active frequency in the service config,
# e.g. { "Name": "daily", "Cron": "0 0 6 * * ?", "Frequency": 1 }
curl -s -X POST "http://localhost:5004/document/subscription/save?identifier=primary" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "Id": "<guid>", "UserId": "<userGuid>", "Mail": "<mail>", "Body": "<body>",
        "Attachment": true, "AttachmentFileName": "<name>.pdf",
        "NotificationId": "<notificationGuid>", "Frequency": 1, "Active": true }'
```

After delivery, `Subscription.LastSendStamp`/`LastError` show the outcome (or use the Service API
below).

## 10. Service API: read + report send result

```bash
SERVICE_TOKEN=$(curl -s -X POST "<IdP-Base>/protocol/openid-connect/token" \
  -d "grant_type=client_credentials" -d "client_id=<service-client>" \
  -d "client_secret=<secret>" -d "scope=serviceApi" | jq -r .access_token)

curl -s "http://localhost:5004/document/notification/notificationmetadatabytenantandid/<tenantGuid>/<guid>" \
  -H "Authorization: Bearer $SERVICE_TOKEN"

curl -s -X POST "http://localhost:5004/document/subscription/setsendresult/<tenantGuid>/<subscriptionGuid>" \
  -H "Authorization: Bearer $SERVICE_TOKEN" -H "Content-Type: application/json" \
  -d '"OK"'
```
