# Aufteilung: Reporting-API (Service) und Blazor-Apps (Ballware.Document.App)

## Kontext

- Proxy `api.ballware.de` routet `https://api.ballware.de/document` → Document-Service (identisches Prefix). Der Blazor-Anteil (Assets, `/_framework`, `/_content`, DX-Resources) erzeugt URLs ohne Prefix → 401 des Gateways. Behelfslösung im Branch: `ReverseProxyOptions.PathPrefix = "/assets/document"` + `UseReverseProxyPathBase` + `base href` (ReverseProxyExtensions.cs:18-58, App.razor:7). Kollidiert mit `/signon`, `/viewer`, `/designer` und iframe-Betrieb.
- Heute hostet `Ballware.Document.Service` API **und** Blazor: `Startup.cs:330-341` (Razor/Antiforgery), `Startup.cs:581-626` (PathBase, Session, Antiforgery, DX-Blazor, `MapRazorComponents`), `Endpoints/SignOnEndpoint.cs`, `Components/*`, `wwwroot/customization.js`.
- Ziel: Service = reine API unter `/document/**` (Prefix bleibt, Kontrakt unverändert). Neuer Dienst `Ballware.Document.App` auf `https://reporting.ballware.de` = `/signon`, `/viewer`, `/designer` + Assets, **ohne** Path-Prefix.

## Festgelegte Entscheidungen

1. **Geteilte Bibliothek `Ballware.Document.Hosting`** (SDK `Microsoft.NET.Sdk`): Adapter, Options, Mapster-Profile, DI-Extensions. Service und App referenzieren sie. Kein Duplizieren, keine App→Service-Projektreferenz.
2. **App greift direkt auf dieselbe DB zu** (EF-Repositories + `IDocumentDbConnectionFactory` für `documentLookup`-Datenquellen und Dokument-Metadaten). `Storage:AutoMigrations` im App = `false` (Migrations bleiben beim Service/`InitializationWorker`).
3. **Auth im App**: `/signon/{idToken}` → `DistributedSession`-Cookie (SameSite=None/Secure). OIDC-Challenge bleibt `DefaultChallengeScheme` für Seitenrequests; Requests auf `/_blazor` (SignalR negotiate/socket) und DX-Transport-Endpoints liefern gezielt **401** statt 302 (Fund #4 des Migrationsplans).
4. **Service** verliert vollständig: Razor-Komponenten, InteractiveServer/Hub, Antiforgery+CSP-Framing, `UseReverseProxyPathBase`/`ReverseProxyOptions`, `/signon`, `AddBallwareSession`/`DistributedSession`. API ist JWT-only.
5. **Single Instance** für den App: In-Process-DistributedCache + In-Process-DataProtection (wie heute), kein Redis, kein Keyring-Volume. Neustart ⇒ Frontend ruft erneut `/signon`.
6. **`redirect` in `/signon`** nur relativ und nur Start mit `/` bzw. `/viewer`/`/designer`; sonst 400 (Open-Redirect geschlossen).
7. **CSP frame-ancestors explizit pro Umgebung** (`ContentSecurity:FrameAncestors`), nicht mehr aus `Cors:AllowedOrigins` abgeleitet.
8. `Ballware.Document.Engine.Dx` bleibt eine RCL, aber Extensions werden in **Core** (beide Hosts) und **Blazor** (nur App) getrennt.

## Arbeitspakete

### WP0 — Gate: API-Auth ohne Cookie im Service

Vor WP2 prüfen, ob `MapDocumentUserApi`/`MapNotificationUserApi`/`MapSubscriptionUserApi`/`MapProcessingStateDocumentApi` (Ballware.Shared.Api.Endpoints, `RequireAuthorization` mit Scope/Policy) nach dem Entfernen von Cookie-/Session-Auth weiterhin mit Bearer-JWT autorisieren. Falls sie den Default-Scheme verwenden: im Service `DefaultScheme = DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme` setzen und `AddPolicy` für die genutzten Scopes mit `AddAuthenticationSchemes(JwtBearer)` ergänzen. Nachweis: Curl gegen Staging-API mit IdToken (Liste abrufen + Dokument speichern).

### WP1 — `src/Ballware.Document.Hosting` (reines Refactoring)

Neues Projekt (net10.0, `Microsoft.NET.Sdk`), Referenzen: Data, Data.Ef(+Postgres/SqlServer), Api, Metadata, Session, `ballware.shared.authorization(.Jint)`, `Ballware.*.Service.Client`-Pakete, Mapster, Duende, EF/Npgsql.

Verschieben (mit Namespace-Änderung, keine Logikänderung):
- `Service/Configuration/{AuthorizationOptions,CorsOptions,ServiceClientOptions,SwaggerOptions,ConfigurationException}` → `Hosting/Configuration`
- `Service/Adapter/*` (alle 7) → `Hosting/Adapter`
- `Service/Mappings/*` (3 Profile) → `Hosting/Mappings`

Neue Extensions (`Ballware.Document.Hosting`):
- `AddBallwareDocumentOptions(IConfiguration)`: Options-Binding+Validierung Authorization, Storage, Session, Meta/Storage/Generic/MlClient.
- `AddBallwareDocumentServiceClients()`: 4 `AddHttpClient` + `AddClientCredentialsTokenManagement` analog `Startup.cs:346-444`.
- `AddBallwareDocumentDataStore(StorageOptions, connectionString)`: mssql/postgres-Umschaltung analog `Startup.cs:470-477`.
- `AddBallwareDocumentMetadataProviders()`: Scoped-Provider, geteilt: `IDocumentMetadataProvider`, `IMetaDatasourceProvider`, `IDocumentPickvalueProvider`, `IDocumentLookupProvider`, `IDatasourceDefinitionProvider` (generic/meta/ml), `IAuthorizationMetadataProvider`, `IDocumentModificationProvider`-Abhängigkeiten. Service-only: `IDocumentProcessingStateProvider`/`IProcessingStateProvider`/`INotificationMetadataProvider`/`ISubscriptionMetadataProvider`/`IJobMetadataProvider`/`IExportMetadataProvider`/`IFileStorageProvider` + `ITenantableRepositoryHook` (`DocumentImportExportRepositoryHook` bleibt im Service).
- `AddBallwareDocumentAuthentication(AuthorizationOptions, bool withSession)`: JWT immer; `withSession=true` zusätzlich `AddBallwareSession` + `DistributedSession`; `documentApi`-Policy analog `Startup.cs:283-293`; `ConfigurationManager<OpenIdConnectConfiguration>`; `AddBallwareSharedAuthorizationUtils`; DataProtection mit **getrennten** ApplicationNames (`ballware-document-service` / `ballware-document-app`).
- `MapBallwareDocumentSignOnEndpoint(this IEndpointRouteBuilder)`: SignOn-Logik mit Redirect-Validierung (`Uri.TryCreate(redirect, UriKind.Relative)` + `PathString.StartsWithSegments("/viewer"|"/designer")`, sonst 400).

Anpassungen: Service-csproj `ProjectReference` Hosting; `using` in `Startup.cs`/Adapter-Nutzern ersetzen; `Service/Configuration/*` und `Service/Mappings/*` löschen.
Check: `dotnet build ballware-document-service.sln` + `dotnet test` (20+16+16) grün, Verhalten unverändert.

### WP2 — Service entrümpeln (nur API)

Entfernen (Code + Dateien):
- `Startup.cs`: `AddRazorComponents/AddInteractiveServerComponents/AddHubOptions`, `Configure<AntiforgeryOptions>`, `ResolveFrameAncestorsPolicy`/`NormalizeFrameAncestorsPolicy`/`FramingFromForeignOriginsAllowed`, `UseReverseProxyPathBase`, `UseAntiforgery`, `MapRazorComponents<App>()`, `MapSignOnEndpoint()`, `AddBallwareSession`, `AddCookie()`, `AddOpenIdConnect(...)`, `AddDistributedMemoryCache` (nur wenn nichts anderes sie braucht).
- Dateien: `Components/App.razor`, `Components/Routes.razor`, `Components/Layout/MainLayout.razor`, `Components/_Imports.razor`, `wwwroot/customization.js`, `Endpoints/SignOnEndpoint.cs`, `Extensions/ReverseProxyExtensions.cs`, `Configuration/ReverseProxyOptions.cs`.
- `appsettings.json`: `ReverseProxy`-Block streichen. `appsettings.Development.json`: Components/SignalR/DevExpress-Debug-Einträge streichen.
- Authentication: `DefaultScheme`/`DefaultChallengeScheme` = `JwtBearer` (WP0-Ergebnis maßgeblich).

`Engine.Dx`-Extensions splitten:
- `AddBallwareDevExpressReportingCore()`: `LoggerService`, 3 `Fetch*`-Singletons, `IDocumentDatasourceProvider`, `IDocumentModificationProvider`, `IDocumentMailGenerator`, `ConfigureReportingServices(... EnableCustomSql())` — **ohne** `AddDevExpressBlazorReporting`, ohne `AsyncModeSettings`, ohne `ReportStorageWebExtension`.
- `AddBallwareDevExpressReportingBlazor()`: core + `AddDevExpressBlazorReporting()` + `AsyncModeSettings(true)` + `AddScoped<ReportStorageWebExtension, DocumentStorage>` (Reihenfolge wie ServiceCollectionExtensions.cs:24-33).
- `UseBallwareDevExpressReportingCore()` (Logger-Init + `CriteriaOperator.RegisterCustomFunction`) bzw. `...Blazor()` (core + `UseDevExpressBlazorReporting()`).

Service nutzt nur Core; Quartz-Jobs (`SubscriptionTriggerJob`, `DocumentUpdateDatasourcesJob`, `DocumentMailGenerator.ExportToPdfAsync`) bleiben funktionsfähig (DX Drawing/Skia bleiben in Engine.Dx).

### WP3 — `src/Ballware.Document.App` (neuer Host)

Projekt: SDK `Microsoft.NET.Sdk.Web`, net10.0, `OutputType=Exe`, `IsPackable=false`. Referenzen: Hosting, Engine.Dx, Session. Pakete: DevExpress.Blazor + DevExpress.Blazor.Themes.Fluent + DevExpress Blazor Reporting JS-BasedControls (via Engine.Dx), OpenIdConnect, Serilog/Serilog.AspNetCore, SkiaSharp.NativeAssets.Linux.

- `Program.cs`: wie aktuelles Service-`Program.cs` (Konfigreihenfolge, Serilog, Kestrel-Options).
- `Components/App.razor`: `<base href="/" />` (keine Options-Abhängigkeit), `@DxResourceManager.RegisterTheme(Themes.Fluent)`, `RegisterScripts(new DxResource("customization.js", 900))`.
- `Components/Routes.razor`, `Components/Layout/MainLayout.razor`, `Components/_Imports.razor` aus dem Service übernehmen (Namespaces: `Ballware.Document.App[.Components[.Layout]]`, `Ballware.Document.Hosting.Configuration`, DX-Namespaces).
- `wwwroot/customization.js` aus dem Service übernehmen (`document.baseURI`-Variante).
- `Startup.cs`: Hosting-Extensions (Options/Clients/DataStore/MetadataProvider-Core/Auth+Session) + `AddRazorComponents().AddInteractiveServerComponents(DetailedErrors).AddHubOptions(MaximumReceiveMessageSize = 256*1024)` + `AddBallwareDevExpressReportingBlazor()` + `Configure<AntiforgeryOptions>` mit `SuppressXFrameOptionsHeader = true` und `Cookie.Name = ".Ballware.Antiforgery"`.
- Pipeline-Reihenfolge: ExceptionHandler (ProblemDetails-JSON) → `UseForwardedHeaders` → `UseRouting` → `UseBallwareSession` → Challenge-Filter (WP4) → `UseAuthentication` → `UseAuthorization` → `UseAntiforgery` → `UseStaticFiles` → Kultur `de-DE` → `UseBallwareDevExpressReportingBlazor()` → `MapBallwareDocumentSignOnEndpoint()` → `MapRazorComponents<App>().AddAdditionalAssemblies(typeof(Engine.Dx.Components.Designer).Assembly).AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = <konfiguriert>)`. Kein `UseReverseProxyPathBase`, kein Prefix, `PathBase` bleibt leer.
- `appsettings.json` (App): Kestrel-Limits wie Service, Serilog, `Authorization` (Authority `https://identity.ballware.de/realms/ballware`, Audience, ClientId, Claims, `RequiredUserScope`), `Storage` (`AutoMigrations: false`), `MetaClient`/`StorageClient`/`GenericClient`/`MlClient`, `Session`, `ContentSecurity:FrameAncestors` (z. B. `'self' https://app.ballware.de`), `ConnectionStrings`. `appsettings.Development.json`: `DetailedErrors: true`, Debug-Logging Components/SignalR/DevExpress, Kestrel `http://localhost:5005`.

### WP4 — Challenge-Differenzierung + Redirect-Härtung

- `ContentSecurity:FrameAncestors` Pflichtwert je Umgebung (Operator pflegt die Frontend-Origin(s)); Fallback `'self'` + `X-Frame-Options: SAMEORIGIN`, wenn leer.
- Kleine Middleware vor `UseAuthentication`: Pfadpräfixe `/_blazor` (und DX-Transport-/Export-Endpoints) ⇒ fehlende Auth = 401 statt OIDC-302; Seiten (`/viewer`, `/designer`) ⇒ OIDC-Challenge wie bisher (`/signin-oidc` → Cookie).
- SignOn-Redirect: relativ + `/`|`/viewer`|`/designer` (WP1), sonst 400.

### WP5 — Solution, Docker, CI

- `ballware-document-service.sln`: `Ballware.Document.Hosting` und `Ballware.Document.App` aufnehmen (alle Solution-Configuration-Referenzen).
- `src/Ballware.Document.App/Dockerfile`: analog Service-Dockerfile (dotnet/aspnet:10.0 + sdk:10.0, debconf-Fonts, `libicu-dev`, `libfontconfig1`, `ttf-mscorefonts-installer`, `libjpeg-turbo8`, `libjpeg62`, `GITHUB_USERNAME`/`GITHUB_PAT`/`DEVEXPRESS_NUGETFEED`), COPY-Liste inkl. `Ballware.Document.Hosting` + `Ballware.Document.App`, `ENTRYPOINT ["dotnet","Ballware.Document.App.dll"]`.
- `.github/workflows/publish-packages.yml`: zweiter build-push-Step (identische Buildx/QEMU-Konfiguration) mit `context: src/.`, `file: src/Ballware.Document.App/Dockerfile`, Tags `ghcr.io/ballware/ballware-document-app:{semver}` und `{stable|latest}`. `dotnet pack` veröffentlicht zusätzlich `Ballware.Document.Hosting` (App bleibt `IsPackable=false`). `sonarqube-latest.yml` auf neue Projekte prüfen.

### WP6 — Gateway-/Keycloak-/Frontend-Vertrag (Außeneffekte, abzustimmen)

- Gateway: neuer Virtual Host `reporting.ballware.de` → App, keine Prefix-Remapping, WebSocket-Upgrade für `/_blazor` durchlassen. `api.ballware.de` routet weiterhin nur `/document/**` (+ Swagger) zum Service; `/signon`, `/viewer`, `/designer` werden dort nicht mehr geroutet.
- Keycloak-Client des Frontends: Redirect-URI `https://reporting.ballware.de/signin-oidc` registrieren + Allowed Web Origins `https://reporting.ballware.de` (sonst Fund-#4-Fehler `invalid_request: Invalid parameter: redirect_uri`).
- iframe-Kontrakt (out-of-repo, Frontend muss umgestellt werden): `https://reporting.ballware.de/signon/{idToken}?redirect=%2Fviewer%3FdocId%3D…%26id%3D…` bzw. `…?redirect=%2Fdesigner%3Fid%3D…`; `redirect` URL-codiert, relativer Pfad inkl. Query.
- Session-Cookie bleibt `SameSite=None; Secure` (dann `__Host-`-Präfix entfernen); CORS wird nicht benötigt (iframe-Navigation + SignalR mit Credentials).

## Risiken

- **API-Auth nach Cookie-Entfernung** (WP0) — wichtigste unkalkulierbare Stelle; vor WP2 verifizieren.
- App und Service teilen dieselbe DB: App schreibt Dokument-Metadaten und Storage-Attachments ⇒ identische Berechtigungen/ConnectionStrings nötig; Migrations-ownership nur Service.
- App braucht Netzwerkzugriff auf die Tenant-Fachdatenbanken (Designers/Viewer führen Report-Queries selbst aus) — andere Netzwerkzone als bisher.
- SignalR-Circuit kann einer 302 nicht folgen; Frontend muss auf 401/404 mit erneutem `/signon` + iframe-Reload reagieren.
- Zwei Hosts laden DX-Server-Rendering: App benötigt Fonts/Skia (Docker), Service ebenso (Mail-Jobs).
- `SignOnEndpoint`-Validierung ändert Verhalten für Bestandsaufrufe mit absolutem `redirect` (bewusst).

## Validierung

1. `dotnet build ballware-document-service.sln` + `dotnet test` grün (nach WP1 und WP2 unverändert).
2. Service-Smoke: Start ohne Razor, Swagger `/swagger/document/swagger.json` lesbar; API-Calls mit Bearer (Select-List, Save, Notification/Subscription, ProcessingState) → 200; ohne Token → 401.
3. Service-Jobs: Mail-Trigger erzeugt PDF-Attachment; `DocumentUpdateDatasourcesJob` aktualisiert Datenquellen (Core-Registration ohne Blazor).
4. App lokal: `signon → /viewer?docId=…&id=…` rendert Bericht; `/_framework/blazor.web.js`, `/_content/DevExpress.Blazor.Reporting.JSBasedControls/...`, `/dx/Localization/dx-reporting.de.json`, `/customization.js` = 200; kein Prefix in den URLs.
5. App-Sicherheit: `signon` mit `redirect=https://evil.test` → 400; ohne Cookie → 302 OIDC auf `/viewer`, 401 auf `/_blazor/negotiate`.
6. Designer: `new`-Bericht + Tenant-Datenquellen, Speichern (neue GUID), Berechtigungsmatrix `meta.document.edit`, ausgeblendete Menüpunkte, deutsche UI; Custom-Functions in Berichten; Viewer-Export PDF/Office.
7. Staging im echten Frontend-iframe inkl. WebSocket-Upgrade und frame-ancestors-Header (kein XFO).
8. CI: beide Images baubar (amd64+arm64) mit Feed-Secrets.

## Offene Punkte

- Frontend (separates Repo) muss Basis-URL `https://reporting.ballware.de` konfigurieren und die iframe-Aufrufe umstellen — nicht Teil dieses Repos.
- Gateway-Konfiguration (Host, WebSocket) und Keycloak-Client-URI (WP6) sind Betriebsaufgaben vor dem Cut-over.
- Cut-over ist atomar geplant (Service verliert Blazor, App übernimmt): Reihenfolge Gateway→App-Deployment→Frontend; Rollback nur über alten Stand.
