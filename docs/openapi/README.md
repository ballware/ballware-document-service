# OpenAPI snapshots

The committed files in this folder are the source of truth the API pages were written against:

| File | Content |
| --- | --- |
| [document.json](document.json) | `ballware document user API` — 44 paths, 9 component schemas, OpenAPI `3.0.4` |
| [service.json](service.json) | `ballware document service API` — 9 paths, 4 component schemas, OpenAPI `3.0.4` |
| [manifest.json](manifest.json) | Provenance: generator tool + version, environment, git describe reference, `fetchedAt`, per-document openapi/title/pathCount/schemaCount |
| [fetch-spec.sh](fetch-spec.sh) | Generator script |

## How the snapshot is produced

Generation runs **offline from the built assembly** — no database, no IdP:

```bash
docs/openapi/fetch-spec.sh
```

What it does:

1. `dotnet build src/Ballware.Document.Service --configuration Release`
2. Ensures the tool is available — global install guarded by a PATH check:
   `dotnet tool install Swashbuckle.AspNetCore.Cli --version 10.3.0 --global`
   (the CI alternative: `dotnet new tool-manifest && dotnet tool update Swashbuckle.AspNetCore.Cli --version 10.3.0`,
   invoked via `dotnet tool run swagger …`)
3. Exports both documents with the Swagger environment:

   ```bash
   export ASPNETCORE_ENVIRONMENT=Swagger
   export Authorization__Authority=https://localhost
   cd src/Ballware.Document.Service
   swagger tofile --output ../../docs/openapi/document.json bin/Release/net10.0/Ballware.Document.Service.dll document
   swagger tofile --output ../../docs/openapi/service.json  bin/Release/net10.0/Ballware.Document.Service.dll service
   ```

4. Validates each file (parses as JSON, `openapi` starts with `3.`, `paths` non-empty) and writes
   `manifest.json`.

The `swagger tofile` tool resolves the host from the assembly **without starting it**: service
registration runs (including the DevExpress reporting core registration), but the host is never
started — no EF migration, no Quartz startup, no Postgres, no IdP needed. Offline export has been
verified working with the private NuGet feed and DevExpress license present at build time.

## Why `ASPNETCORE_ENVIRONMENT=Swagger`

`appsettings.Swagger.json` (`src/Ballware.Document.Service/appsettings.Swagger.json`) supplies
**fake** Authority/clients (`https://localhost`, `fake*` clients/secrets), fake Meta/Storage/
Generic/ML clients, fake SMTP settings and `Quartz.Simpl.RAMJobStore` (the production
`appsettings.json` uses a clustered AdoJobStore). Consequently the `oidc` security definition in
the snapshot points at a non-existent endpoint:

```json
"oidc": { "type": "openIdConnect", "openIdConnectUrl": "https://localhost/.well-known/openid-configuration" }
```

That URL is a placeholder, **not** a real IdP discovery endpoint. Do not wire clients to it.

> **Leak warning:** configuration order is
> `appsettings.json` → `appsettings.{Environment}.json` → `appsettings.local.json` → environment
> variables (`src/Ballware.Document.Service/Program.cs:12-16`). A developer
> `appsettings.local.json` overrides the Swagger values — including the Authority — and its real
> IdP URL would be baked into the committed snapshot. The script therefore pins
> `Authorization__Authority=https://localhost` as an environment override. When regenerating by
> hand (or in CI), generate from a clean checkout without `appsettings.local.json`.

## Tool version pin

Keep `Swashbuckle.AspNetCore.Cli` at **10.3.0** — it matches the service's Swashbuckle packages
(`src/Ballware.Document.Service/Ballware.Document.Service.csproj:20-21`) and the CI tool pin
(`.github/workflows/publish-packages.yml`). Other versions may reformat the documents and create
spurious diffs.

## Refresh cadence and drift

The snapshot is a manual artifact: regenerate after endpoint changes (new/renamed routes, DTO
changes, Swagger config). A re-run should only change `manifest.json`'s `fetchedAt`; if
`document.json`/`service.json` also change, review the diff — it is an API surface change and the
API pages here should be reviewed against it.

## Live-server fallback

If `swagger tofile` ever fails to resolve/initialize the host (e.g. a DevExpress assembly or
license load error), generate from a running instance instead:

```bash
ASPNETCORE_ENVIRONMENT=Swagger dotnet run --project src/Ballware.Document.Service
curl -s http://localhost:5004/swagger/document/swagger.json -o docs/openapi/document.json
curl -s http://localhost:5004/swagger/service/swagger.json  -o docs/openapi/service.json
```

Prefer the offline path; a live-server export inherits the developer's local configuration. Do
not weaken production wiring to make offline export work.

## Snapshot ↔ docs mapping

| Snapshot | Documented by |
| --- | --- |
| `document.json` paths + schemas | [../api/generic-crud.md](../api/generic-crud.md), [../api/document-api-resources.md](../api/document-api-resources.md), document tables in [../api/schemas.md](../api/schemas.md) |
| `service.json` paths + schemas | [../api/service-api.md](../api/service-api.md), service tables in [../api/schemas.md](../api/schemas.md) |
| both | [../api/examples.md](../api/examples.md) (every example path exists in the snapshots) |
