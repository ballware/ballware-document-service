# API surface and conventions

Both OpenAPI documents belong to the **Service host** (`Ballware.Document.Service`, dev port
`5004`). Swagger UI: `http://localhost:5004/swagger`, raw documents at
`/swagger/document/swagger.json` and `/swagger/service/swagger.json`.

| Document | Title | Version | Paths | Schemas | Scope → policy |
| --- | --- | --- | --- | --- | --- |
| [document.json](../openapi/document.json) | ballware document user API | `v1` | 44 | 9 | `documentApi` |
| [service.json](../openapi/service.json) | ballware document service API | `v1` | 9 | 4 | `serviceApi` |

Regeneration: [../openapi/README.md](../openapi/README.md) / [../openapi/fetch-spec.sh](../openapi/fetch-spec.sh).

## Groups vs. tags

- **Group** (`WithGroupName`) decides *which OpenAPI document* an operation lands in:
  `"document"` for all user operations, `"service"` for inter-service operations
  (`src/Ballware.Document.Service/Startup.cs:218-230`).
- **Tag** (`WithTags`) drives the category grouping in Swagger UI and downstream doc
  generators: `Document`, `Notification`, `Subscription`, `ProcessingState`.

All `/document/**` user operations (generic CRUD included) are in the `document` document.

## Schema naming

No `CustomSchemaIds` are configured — component schema names are the plain CLR type names
(`Document`, `DocumentSelectListEntry`, `ExportUrlResult`, …). Schemas shared between the two
documents (`Document`, `Notification`, `Subscription`, `DocumentSelectListEntry`) are duplicated
per document, because each Swagger document is self-contained.

## Content types

- Request/response JSON bodies are **PascalCase** (`Startup.cs:85-93`).
- `POST save`/`savebatch` take `application/json`; `export` responses are declared as a string
  in the snapshot but at runtime carry the export definition's media type; `exporturl` accepts
  `application/x-www-form-urlencoded`; `import` accepts `multipart/form-data` (field `files`);
  `download` returns the exported file as `application/json` payload (the export media type).
- `updatedatasources` and `subscription/trigger` have **no request body** — ids are repeated
  `id` query params (`DocumentMetaEndpoint.cs:189-212`, `SubscriptionMetaEndpoint.cs:148-171`).

## Authentication

Security scheme `oidc` (`openIdConnectUrl = <Authority>/.well-known/openid-configuration`) is
attached to every operation with the scopes from `Swagger:RequiredScopes`
(`openid documentApi serviceApi` in the Swagger profile).

Swagger UI signs in via PKCE against the configured `Swagger:ClientId`. For scripting, fetch a
machine token with client credentials against your IdP:

```bash
TOKEN=$(curl -s -X POST "<IdP-Base>/protocol/openid-connect/token" \
  -d "grant_type=client_credentials" -d "client_id=<client>" \
  -d "client_secret=<secret>" -d "scope=serviceApi" | jq -r .access_token)
```

(Use `scope=documentApi` and a user context for the user document.)

## Anonymous exception

`GET /document/<entity>/download/{tenantId}/{id}` is the only anonymous operation — the export
id is the capability (`TenantantableEditingEndpoint.cs:125-131`). See
[generic-crud.md](generic-crud.md#export--exporturl--download).

## Further reading

- [generic-crud.md](generic-crud.md) — the 11 shared editing operations
- [document-api-resources.md](document-api-resources.md) — user-specific operations
- [service-api.md](service-api.md) — the 9 Service API operations
- [schemas.md](schemas.md) — component schema field tables
- [examples.md](examples.md) — copy-pasteable sequences
- [best-practices.md](best-practices.md) — anonymized payload/rights templates
