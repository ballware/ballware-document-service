# Best practices

Anonymized templates following the **conventions** the platform uses for a document workflow.
All values are placeholders.

> Identifiers, rights and states are **tenant metadata maintained in the Meta Service**
> (`meta.<entity>.<right>` rights candidates and per-entity query definitions), not hardcoded by
> this service. Concrete tenant configurations are intentionally not reproduced here.

## Query/save identifier set

Conventional identifiers used across query/save/export/import operations (each selects an entry
of the entity's embedded query/save definitions):

| Identifier | Convention |
| --- | --- |
| `primary` | default read/query/save path; rights fall back to `view`/`add`/`edit` |
| `exportjson` | export payload definition (JSON document envelope) |
| `importjson` | import payload definition (same envelope, fed back) |
| `state` | guarded state-transition save; the rights script decides allowed transitions |

## Export/import payload pair

An exported document payload (from `GET /document/document/export?identifier=exportjson&id=<guid>`)
and its re-import through `import?identifier=importjson` use the same JSON envelope. On the
export side `ReportBinary` is carried as a **hex string**; the identical hex string is sent back
unchanged through `importjson` — no re-encoding in between:

```jsonc
{
  "Id": "<guid>",
  "DisplayName": "<display name>",
  "Entity": "<entity identifier>",
  "State": <state number>,
  "ReportBinary": "<hex-encoded report definition>",
  "ReportParameter": "{\"<parameterName>\":<parameterValue>}"   // embedded JSON string
}
```

```bash
# export
curl -s "<base>/document/document/export?identifier=exportjson&id=<guid>" -H "Authorization: Bearer $TOKEN" -o doc.json
# re-import (multipart form field 'files')
curl -s -X POST "<base>/document/document/import?identifier=importjson" -H "Authorization: Bearer $TOKEN" -F "files=@doc.json"
```

## Rights set for a document workflow

Typical right candidates a role must hold to work with documents end-to-end (full candidates are
`meta.document.<right>`):

```text
view              add              edit             delete
exportjson        importjson       state
visiblestate.<n>  ...              (one per state the role may see)
```

- `visiblestate.<n>` gates document visibility in
  `selectlistdocumentsforentity/{entity}` and allowed processing-state successors.
- `state` gates guarded transitions via `save?identifier=state`; pair it with the tenant's
  processing-state metadata (`Locked`, `Finished`, `ReasonRequired` semantics — see
  [concepts/document-model.md](../concepts/document-model.md#processing-states)).

## Subscription record template

```jsonc
{
  "Id": "<guid>",
  "UserId": "<user guid>",
  "Mail": "<recipient mail>",
  "Body": "<mail body text>",
  "Attachment": true,
  "AttachmentFileName": "<file name>.pdf",
  "NotificationId": "<notification guid>",
  "Frequency": <frequency bucket matching a Trigger:Active entry>,
  "Active": true
}
```

Keep `Frequency` aligned with the service's `Trigger:Active` configuration (e.g. `1` for a daily
cron) — a subscription whose frequency matches no trigger is never scheduled automatically, only
via `POST /document/subscription/trigger`.

## Notification + `DocumentParams` template

```jsonc
{
  "Id": "<guid>",
  "Identifier": "<tenant-local identifier>",
  "Name": "<subject line>",
  "DocumentId": "<document guid>",
  "State": <state number>,
  "DocumentParams": "[{\"Name\":\"<report parameter name>\",\"Value\":\"<value>\"}]"
}
```

`DocumentParams` is deserialized as a list of report parameters before rendering
(`SubscriptionTriggerJob`), so each entry maps to one report parameter of the bound document.

## Practical notes

- Prefer select-list endpoints for pickers and generic `byid` for full payloads — they have
  different shapes by design ([schemas.md](schemas.md)).
- For bulk document moves, compute the common target set first with
  `processingstate/selectlistallowedsuccessorsforentities/document?id=…&id=…` (intersection
  semantics), then post `save?identifier=state` per document.
- Use `exporturl` + anonymous `download` for mail/link-based delivery of exports instead of
  streaming `export` through an intermediate; remember the 24 h validity.
- Keep import/export round-trips byte-exact for `ReportBinary` (hex on both ends).
