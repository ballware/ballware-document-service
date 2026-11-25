# Tenancy and authorization

## Claims

Tokens are JWTs issued by the configured OIDC authority (`Authorization:Authority`). The
service reads (defaults, `AuthorizationOptions` in
`src/Ballware.Document.Hosting/Configuration/AuthorizationOptions.cs`):

| Claim | Config key | Used for |
| --- | --- | --- |
| `tenant` | `TenantClaim` | current tenant for every user API handler (`IPrincipalUtils.GetUserTenandId`) |
| `sub` | `UserIdClaim` | acting user (save, import, jobs) |
| `right` | `RightClaim` | repeated right claims evaluated by the rights checker |
| `scope` | – | space-separated scopes checked by the endpoint policies |

Service API handlers do **not** read a tenant claim — they take explicit `{tenantId}` path
segments and require the machine scope `serviceApi`.

## Scopes → policies

Both hosts register two authorization policies — the Service host in
`AddBallwareDocumentApiAuthentication`
(`src/Ballware.Document.Hosting/ServiceCollectionExtensions.cs:324-326`), the App host through
`Ballware.Shared.Web.Oidc` with `Authorization:UserScopePolicyName` /
`Authorization:ServiceScopePolicyName`:

| Policy | Requires | Used by |
| --- | --- | --- |
| `documentApi` | authenticated + `scope` contains `Authorization:RequiredUserScope` (default `documentApi`) | all `/document/**` user operations, incl. generic CRUD |
| `serviceApi` | authenticated + `scope` contains `serviceApi` | the 9 Service API operations |

The Swagger documents attach the `oidc` security scheme with the required scopes from
`Swagger:RequiredScopes` (`Startup.cs:148-154`). A failed policy or a denied right results in
`401`.

## Right candidates

The endpoint policy only checks the scope. Fine-grained access is decided by the tenant's
rights scripts (Jint scripts stored in the Meta Service, evaluated via
`Ballware.Shared.Authorization.Jint`). Generic editing handlers compute a right candidate of
the form `meta.<entity>.<right>` from the mount's `application`/`entity`
(`Startup.cs:220,224,228`) and the query `identifier`:

| Operation | Right candidate |
| --- | --- |
| `all`, `query`, `byid` | `meta.<entity>.view` |
| `new` | `meta.<entity>.add` |
| `save`, `savebatch` (identifier `primary` or absent) | `meta.<entity>.edit` |
| `save`, `savebatch` (non-`primary` identifier) | `meta.<entity>.<identifier>` |
| `remove/{id}` | `meta.<entity>.delete` |
| `export`, `exporturl` | `meta.<entity>.<identifier>` |
| `import` | scope only at request time; the import job checks `meta.<entity>.<identifier>` per item |

(`ballware-shared-dotnet/src/Ballware.Shared.Api.Endpoints/TenantableEndpointHandlerFactory.cs`,
right names at `:22-25`.)

Identifiers, rights and processing states are **tenant metadata maintained in the Meta
Service** — they are not hardcoded by this service.

## State visibility

`GET /document/document/selectlistdocumentsforentity/{entity}` filters the result list per
state: for each candidate document the handler evaluates the right candidate
`visiblestate.<state>` against the entity metadata (`entityMeta`) and the user's `right`
claims (`src/Ballware.Document.Api/Endpoints/DocumentMetaEndpoint.cs:129-136`). Documents in
states the user may not `visiblestate.<n>` are simply absent from the response.

The processing-state successor endpoints apply the same mechanism through
`IEntityRightsChecker.StateAllowedAsync` to the candidate successor states
(`ProcessingStateMetaEndpoint.cs:67,111`).

## Status-code matrix

| Code | When |
| --- | --- |
| `401` | missing/invalid bearer token, missing required scope, denied right candidate |
| `404` | tenant metadata not found, entity metadata not found, item not found (`byid`, `selectbyid`, service-API lookups), export record missing or expired (`download`) |
| `400` | `remove/{id}` precondition scripts reported failures (joined messages as body); missing `id` query params on `updatedatasources` / `subscription/trigger` |
| `201` | `import`, `updatedatasources`, `subscription/trigger` accepted (processing happens in jobs) |
| `500` | unhandled exception as `application/problem+json` (details only in Development, `Startup.cs:167-192`) |

## App host sign-on and the browser-vs-transport distinction

The App host authenticates browsers through OIDC; the cookie/OpenID Connect stack, the sign-on
endpoint and the challenge middlewares are provided by the shared packages
`Ballware.Shared.Web.Oidc` / `ballware.shared.web.session` and wired up by
`AddBallwareDocumentBrowserAuthentication`:

- `GET /signon/{idToken}?redirect=…` validates the id token against the IdP signing keys
  (`ValidAudience = ClientId`), establishes the distributed session and redirects. `redirect`
  must be relative and start with `/` or one of `Authorization:SignOnAllowedRedirectPathPrefixes`
  (`/viewer`, `/designer`); anything else is `400`
  (`Ballware.Shared.Web.Oidc/SignOnEndpointExtensions.cs`).
- `Authorization:PublicBaseUrl` makes the OIDC `redirect_uri` deterministic;
  `SignOnAllowedHosts` restricts redirect targets.

Interactive DevExpress/Blazor circuits are **not** browser navigations. The shared
`ChallengeDifferentiationMiddleware` classifies requests for `/_blazor`, `/DXX` and `/dxDR`
(Blazor circuit + DevExpress export/viewer transports) as transport requests: instead of a
302 OIDC challenge inside a circuit they receive `401`, so the frontend can re-enter via
`/signon`. `SignInChallengeFallbackMiddleware` converts authorization challenges for browser
requests.

These transport endpoints are not part of the OpenAPI documents.
