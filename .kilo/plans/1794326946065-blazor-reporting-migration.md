# Migration DevExpress Reporting → Blazor + Update 26.1 / .NET 10

## Kontext

- `Ballware.Document.Engine.Dx` (Razor Class Library, net8.0) hostet Razor Pages `/designer` + `/viewer` (`Pages/Designer.cshtml`, `Pages/Viewer.cshtml`), die webpack-Knockout-Bundles (`wwwroot/dist/*-script.bundle.js`) laden; webpack/npm laufen über MSBuild-Targets `NpmInstall`/`WebpackBuild` im csproj.
- DX-Endpoints: `Controllers/ReportDesignerController.cs` (eigener `GetReportDesignerModel` mit Tenant-Datenquellen + Berechtigungen `meta.document.edit`), `WebDocumentViewerController.cs`, `QueryBuilderController.cs` — alle `[Authorize(AuthenticationSchemes = "DistributedSession")]`.
- Auth: `/signon/{idToken}?redirect=...` (SignOnEndpoint) → ClaimsPrincipal in DistributedCache + Session-Cookie (SameSite=None, Secure) → `CachedClaimsPrincipalMiddleware` + Schema `DistributedSession`. Designer/Viewer werden per iframe (User Delegation) aus dem ballware-Frontend eingebettet.
- `DocumentStorage : ReportStorageWebExtension` (global registriert) löst Report-URLs: GUID, `docId=…&id=…`-Print-URLs, `new`; Tenant/User via `IHttpContextAccessor`.
- Deployment: Single Instance (In-Memory-DistributedCache), Docker mit privatem DevExpress-NuGet-Feed (`DEVEXPRESS_NUGETFEED`), CI auf dotnet sdk 8.0 + Node 20.

## Festgelegte Entscheidungen

1.DevExpress-Lizenz deckt Blazor ab (bestätigt).
2. JS-basierte Blazor-Wrapper: `DxReportDesigner` + `DxDocumentViewer` (`DevExpress.Blazor.Reporting.JSBasedControls`), Rendermode **InteractiveServer** (SignalR), volle Parität zum Knockout-Client.
3. Blazor-Host im bestehenden `Ballware.Document.Service`; URLs `/designer` und `/viewer`, SignOn-/Session-Logik bleiben unverändert.
4. .NET 10 (net10.0) für die **gesamte** Solution; DevExpress-Packages auf latestes verfügbares 26.1.x-Patch (privater Feed).
5. Harter Schnitt: Knockout/webpack-Client, Razor Pages und DX-MVC-Controller werden ersatzlos entfernt.
6. Single Instance → SignalR-Sticky-Sessions unkritisch; WebSocket-/blazor muss am Reverse-Proxy durchgelassen werden.

## Arbeitspakete (Reihenfolge)

### WP1 — .NET-10-Grundupgrade (ohne DX-Funktionsänderung)
1. Alle `src/**` + `test/**` csproj: `net8.0` → `net10.0`.
2. Package-Bumps auf 10.x-Stände: `Microsoft.AspNetCore.Authentication.JwtBearer`, `...OpenIdConnect`, `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `Microsoft.Extensions.*`, `Microsoft.AspNetCore.Mvc.NewtonsoftJson`; Serilog/Quartz/Swashbuckle/Duende auf aktuelle kompatible Stände ziehen.
3. Kompatibilität der privaten `Ballware.Shared.*`- und `Ballware.*.Service.Client`-Pakete (GitHub-Feed) prüfen; ggf. Version anheben (Checkpoint: net10-kompatibel?).
4. `.github/workflows/publish-packages.yml`: `dotnet/sdk:8.0`-Image und `node-version: 20`-Step entfernen/benennen; `sonarqube-latest.yml` auf dotnet 10 prüfen.
5. `dotnet build` + `dotnet test` grün.

### WP2 — DevExpress 26.1 + Entfernen des webpack-Stacks
1. `Ballware.Document.Engine.Dx.csproj`: `DevExpress.AspNetCore.Reporting`, `DevExpress.Drawing`, `DevExpress.Drawing.Skia`, `DevExpress.Pdf.SkiaRenderer`, `DevExpress.Web.Reporting.Common` auf aktuelles 26.1.x (privater Feed; Build-Check `dotnet restore`).
2. Entfernen: `NpmInstall`/`WebpackBuild`-Targets, `package.json`/`package-lock.json`, `webpack.config.js`, `designer.script.js`, `viewer.script.js`, `node_modules`, `wwwroot/dist`-Bundles, Root-`.nvmrc`.
3. Entfernen: `Pages/Designer.cshtml(.cs)`, `Pages/Viewer.cshtml(.cs)`, `Controllers/ReportDesignerController.cs`, `Controllers/WebDocumentViewerController.cs`, `Controllers/QueryBuilderController.cs`; in `ServiceCollectionExtensions` `AddRazorPages().AddApplicationPart(...)` entfernen (`AddControllers` bleibt für die API).
4. API-Bruchstellen gegen 26.1 bereinigen (entfallene `*.Native.Services`-Typen sind mit den Controllern weg; `DocumentMailGenerator`, `DocumentDatasourceProvider`, `DocumentStorage` kompilieren lassen).
5. `ConfigureReportingServices`: `EnableCustomSql()` behalten; `UseDevelopmentMode` clientseitige Flags (`EnableClientSideDevelopmentMode`, `CheckClientLibraryVersions`) fallen weg — Ressourcen kommen aus den NuGet-Paketen.

### WP3 — Blazor-Integration (Engine.Dx + Service)
1. `Ballware.Document.Engine.Dx.csproj`: `DevExpress.Blazor.Reporting.JSBasedControls` (26.1.x) hinzufügen; SDK bleibt `Microsoft.NET.Sdk.Razor`.
2. Neue Komponenten in `Ballware.Document.Engine.Dx/Components/`:
   - `Designer.razor` — `@page "/designer"`, `@rendermode InteractiveServer`, `@attribute [Authorize]`:
     - `ReportName` = Query-Parameter `id`, sonst `"new"` (via `NavigationManager`).
     - `DataSources` in `OnInitializedAsync` aus `IDocumentDatasourceProvider.CreateDatasourcesForTenant(tenant)` füllen (Tenant via `IPrincipalUtils`/`IHttpContextAccessor`, wie heute).
     - `<DxReportDesignerDataSourceSettings AllowAddDataSource/AllowEditDataSource/AllowRemoveDataSource = rights.Contains("meta.document.edit") />`.
   - `Viewer.razor` — `@page "/viewer"`, `@rendermode InteractiveServer`, `@attribute [Authorize]`:
     - `ReportName` = roher Query-String der Anfrage ohne `?` (ersetzt `decodeURIComponent(urlSearchParams.toString())`); muss die `PrintUrlCheckRegex` in `DocumentStorage.GetData` weiterhin treffen.
   - `_Imports.razor` (DX-Namespaces) in der RCL.
3. Service-Host (`Ballware.Document.Service`):
   - `Components/App.razor` + `Routes.razor` (Router mit `AuthorizeRouteView`), `@DxResourceManager.RegisterTheme(...)` + `@DxResourceManager.RegisterScripts()` im `<head>`; `CascadingAuthenticationState`.
   - `InitializeServices()`: `AddRazorComponents().AddInteractiveServerComponents()`; `AddDevExpressBlazorReporting()`; **danach** `services.AddScoped<ReportStorageWebExtension, DocumentStorage>()` (ersetzt `RegisterExtensionGlobal` in `ApplicationBuilderExtensions`; DokumentStorage bleibt Implementierung).
   - `InitializeApp()`: `app.UseAntiforgery()` (nach `UseAuthorization`), `app.UseDevExpressBlazorReporting()` (ersetzt `UseDevExpressControls()`; `CriteriaOperator.RegisterCustomFunction` + `LoggerService.Initialize` bleiben), `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()`; `MapRazorPages()` entfernen; Reihenfolge `UseBallwareSession` → `UseAuthentication` → `UseAuthorization` beibehalten (wirkt auch auf `/blazor`-Circuit-Requests).
4. Localization: `Localization/dx-*.de.json` als Static Web Asset weiter ausliefern (Base-Pfad `dx` → `/dx/Localization/...`); `wwwroot/customization.js` im Service via `DxResourceManager.RegisterScripts(new DxResource(...))` registrieren mit:
   - `CustomizeLocalization`: `DevExpress.localization.locale('de')` + `e.LoadMessages` der drei JSON-Dateien (fetch statt $.get),
   - `CustomizeMenuActions`: `dxrd-newreport`, `dxrd-newreport-via-wizard`, `dxrd-run-wizard`, `dxrd-exit` weiterhin `visible=false`.
   - Anbindung über `<DxReportDesignerCallbacks>`/`<DxDocumentViewerCallbacks>`.
5. Query-String/URL-Verträge prüfen: `/designer?id=<guid>`, `/viewer?docId=<guid>&id=<guid>…` — Frontend-iframes bleiben unverändert; `/signon/{idToken}?redirect=…` unangetastet.

### WP4 — Build/Docker
1. Dockerfile: Basis-Images auf `dotnet/aspnet:10.0` / `dotnet/sdk:10.0` (aktuell veröffentlichter Tag); Node-Setup entfernen; Mono-Repo-Zeile (stable-buster, EOL) entfernen; `libfontconfig1`, `ttf-mscorefonts-installer`, `libjpeg62-turbo` für Report-Rendering beibehalten; `DEVEXPRESS_NUGETFEED`-Arg bleibt.
2. Release-Image Smoke-Test: Start + Health.

## Risiken / Verifikationspunkte

- **Circuit-Auth**: `@attribute [Authorize]` auf routable Components → 302-Challenge wie heute verifizieren; Alternativpfad: `AuthorizeRouteView` + Redirect auf Prerender. `DocumentStorage`-Tenantauflösung via `IHttpContextAccessor` innerhalb der SignalR-Circuit-Replays explizit testen.
- **Interne DX-Endpoints** (`/blazor`, DX-Export-/Print-Ressourcen): nach Migration prüfen, dass keine unautorisierten Zugriffe auf Reportdaten möglich sind (Cookie-Auth greift über Middleware; DXXRD-artige Endpunkte haben keine eigenen `[Authorize]`-Attribute mehr).
- Reverse-Proxy: WebSocket-Upgrade für `/blazor` + SameSite=None-Cookie im iframe-Setup (https).
- Verfügbarkeit eines 26.1.x-Patches im privaten Feed vor WP2 bestätigen.
- `Ballware.Shared.*`-Pakete evtl. nicht net10-kompatibel (WP1-Checkpoint).
- DevExpress-Blazor-Runtime-Lizenz gemäß DX-Doku konfigurieren (Laufzeitwarnung vermeiden).

## Validierung

1. `dotnet build ballware-document-service.sln` + `dotnet test` (Api.Tests, EF.Tests) grün.
2. Manuelle UAT auf Staging (iframe aus echtem Frontend):
   - SignOn-Redirect → `/designer?id=…` öffnet Bericht; Speichern schreibt Metadaten+Binary; `/designer` ohne `id` öffnet leeren Bericht (`new`-Pfad inkl. Tenant-Datenquellen).
   - Berechtigungen: User ohne `meta.document.edit` sieht keine DataSource-Add/Edit/Remove-UI; User mit Recht darf.
   - Hide-Menüpunkte + deutsche UI (JSON-Localization).
   - `/viewer?docId=…&id=…` Rendern, Parameter-Panel, Print + Export (PDF/Office).
   - Custom-Functions in Berichten (`FetchNameByIdFromLookup`, `FetchTextByValueFromPickvalue`, `FetchNameByStateFromProcessingState`).
   - Mail-Job-Reportgenerierung (DocumentMailGenerator) nach 26.1-Update.
   - Unauthentifizierter Zugriff auf `/designer`/`/viewer` → Challenge/401 wie bisher.

## Offene Fragen

- Keine blockierenden. Implementierung soll zuerst das exakte 26.1.x-Patch und die net10-Kompatibilität der Ballware-Shared-Pakete verifizieren (WP1/WP2-Checkpoint).

## Status (Stand nach Fortsetzung der Umsetzung)

### Erledigt & verifiziert

- WP1: alle `src/**` + `test/**` csproj auf `net10.0`; Package-Bumps (ASP.NET Core 10.0.5, EF/Npgsql 10.x, Serilog 10, Swashbuckle 10.3.0, Duende 4.1.1, `ballware.shared.*` 1.1.0, `ballware.meta.service.client` 1.4.0-alpha.28, u. a.). `AddAutoMapper` → Mapster umgestellt (`TypeAdapterConfig` + `Mapster.DependencyInjection`), da AutoMapper nicht mehr genutzt wird.
  - Check: `dotnet build ballware-document-service.sln` → 0 Fehler; `dotnet test` → 20 (Api.Tests) + 16 (Postgres.Tests) + 16 (SqlServer.Tests) grün.
  - `publish-packages.yml`: `dotnet/sdk:10.0`-Image, Node-Setup entfernt, Swashbuckle.Cli 10.3.0; `sonarqube-latest.yml`: korrekter Project-Key + DevExpress-Feed-Source.
- WP2: DevExpress 26.1.6 im Feed vorhanden und restore/build-fähig. webpack/npm-Stack (`package.json`, `webpack.config.js`, `designer.script.js`, `viewer.script.js`, `.nvmrc`, MSBuild-Targets `NpmInstall`/`WebpackBuild`) sowie Razor Pages und die drei DX-MVC-Controller entfernt; `AddRazorPages().AddApplicationPart(...)`, `AddMvc()`, `UseDevelopmentMode(...)` und `ReportStorageWebExtension.RegisterExtensionGlobal` entfernt; `EnableCustomSql()` bleibt; `ReportStorageWebExtension → DocumentStorage` jetzt per DI (`AddScoped`) nach `AddDevExpressBlazorReporting()` (Reihenfolge laut DX-Doku eingehalten).
- WP3: `Designer.razor` (`/designer`, `id`-Query → sonst `new`, Tenant-Datenquellen, `DxReportDesignerDataSourceSettings` an `meta.document.edit`), `Viewer.razor` (roher Query-String als `ReportName` → trifft `PrintUrlCheckRegex`), `_Imports.razor`, `App.razor` (Theme + `RegisterScripts(customization.js)`), `Routes.razor` (`AuthorizeRouteView` + `CascadingAuthenticationState`), `MainLayout.razor`; Middleware: `UseAntiforgery` nach `UseAuthorization`, `UseDevExpressBlazorReporting()` (Criteria-Functions + LoggerServiceInitialize bleiben), `MapRazorComponents<App>()`.
  - API-Check gegen DX-Doku 26.1: `AddDevExpressBlazorReporting`/`UseDevExpressBlazorReporting`, DI-Registrierung des ReportStorage, `DxReportDesignerDataSourceSettings.Allow{Add,Edit,Remove}DataSource`, `DxReportDesignerCallbacks`/`DxDocumentViewerCallbacks`, `DxResourceManager.RegisterScripts(new DxResource("/customization.js", 900))` — alle korrekt verwendet.
  - Laufzeit-Harness (temporäres Web-Projekt mit RCL-Reference, Stub-Storage/Auth) geprüft: `/designer`, `/viewer?...` = HTTP 200 mit DX-Client-Ressourcen (`dx-reportdesigner.min.js`, `dx-webdocumentviewer.min.js`, Fluent-Theme-CSS); `/dx/Localization/dx-reporting.de.json` (700 kB) und `/customization.js` = 200; `customization.js` wird in der DX-Resource-Pipeline **vor** `dx-reportdesigner.min.js` eingebunden (Callback-Objekt also vor Verwendung definiert). Keine DX-Lizenz-/Startup-Fehler im Log.
- WP4: `dotnet/aspnet:10.0` + `dotnet/sdk:10.0` (Ubuntu 24.04 noble) per Docker validiert; Mono-Zeile und Node-Setup entfernt. Paketverfügbarkeit im Base-Image geprüft: `ttf-mscorefonts-installer` (multiverse), `libfontconfig1`, `libicu-dev`, `libjpeg-turbo8`, `libjpeg62` verfügbar; `libjpeg62-turbo` existiert auf noble **nicht** — die Ersetzung durch `libjpeg-turbo8` + `libjpeg62` ist damit korrekt und wird beibehalten.

### Wichtigster Fund (behoben)

- `MapRazorComponents<App>()` erzeugt Endpoint-Routen nur für Komponenten der App-Assembly; routable Komponenten der RCL (`/designer`, `/viewer`) liefen im Test auf **404**. Fix in `Startup.cs`:
  `app.MapRazorComponents<App>().AddAdditionalAssemblies(typeof(Ballware.Document.Engine.Dx.Components.Designer).Assembly).AddInteractiveServerRenderMode().RequireAuthorization();`
  (danach im Harness: beide Routen 200).

### Fund #2: iframe-Block durch CSP / X-Frame-Options (behoben)

- Symptom im Frontend-iframe: `Framing 'http://localhost:5004/' violates ... "frame-ancestors 'self'"`.
- Ursache (mit net10-Minimal-Harness reproduziert, Header verglichen):
  - `Content-Security-Policy: frame-ancestors 'self'` sendet ASP.NET Core selbst für Interactive-Server-Component-Endpoints (Default von `ServerComponentsEndpointOptions.ContentSecurityFrameAncestorsPolicy`). Razor Pages sendeten diesen Header nicht → Regression durch die Blazor-Umstellung.
  - Zusätzlich `X-Frame-Options: SAMEORIGIN` vom Antiforgery-Middleware (durch WP3.3 neu aktiviert).
- Fix in `Startup.cs`:
  - `AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = FrameAncestorsPolicy)`
  - `Services.Configure<AntiforgeryOptions>(o => o.SuppressXFrameOptionsHeader = FramingFromForeignOriginsAllowed)` (nur wenn framingbreiter als `self`)
  - `FrameAncestorsPolicy`: expliziter Konfigwert `ContentSecurity:FrameAncestors`, sonst aus `Cors:AllowedOrigins` abgeleitet (`*` → `self *`, Kommagetrennliste → `self <origins>`, leer → `self`).
- Verifizierte Header im Harness: `Cors=*` → `frame-ancestors self *` ohne XFO; `Cors=http://localhost:8081,https://app.example.com` → `frame-ancestors self http://localhost:8081 https://app.example.com`; `Cors` leer → `frame-ancestors self` + `X-Frame-Options: SAMEORIGIN`; `ContentSecurity:FrameAncestors` überschreibt.
- Betrieb: Produktions-Deployment sollte die Frontend-Origin(s) in `Cors:AllowedOrigins` (oder explizit `ContentSecurity:FrameAncestors`) pflegen, damit framing nicht pauschal `*` ist.

### Fund #3: Antiforgery-Decryptierungs-Fehler in Containern (dokumentiert, kein Code-Fix)

- Symptom: `AntiforgeryValidationException: The antiforgery token could not be decrypted. ---> CryptographicException: The key {…} was not found in the key ring.`
- Ursache (belegt): Container `reporting-dev` speichert den Data-Protection-Keyring in `/root/.aspnet/DataProtection-Keys` (Start-Warnung "may not be persisted outside of the container"), ohne Volume-Mount → jeder Container-Neubau erzeugt einen neuen Keyring (im Container lag genau ein Key vom Startzeitpunkt). Der Browser bringt ein Antiforgery-Cookie einer vorherigen, zerstörten Instanz mit → Key nicht im Ring.
- Wirkung: `GetCookieTokenDoesNotThrow` schluckt den Fehler und setzt ein frisches Cookie (kein Browser-Fehler, Lograusch beim ersten Request nach jedem Neustart). Betroffen ist aber dasselbe Material, das die Blazor-Circuit-Absicherung (Prerender-State + POST-Validierung) schützt → mit mehreren Instanzen ohne gemeinsamen Keyring/Sticky Sessions wird der Circuit instabil.
- Bewusste Entscheidung: keine Implementierung einer Keyring-Persistenz (Redis/Dateivolume) im Service; nur Dokumentation. Betreibungs-Empfehlung: bei mehreren Replica Keyring teilen (Redis via bestehendem `Redis__ConnectionString`) und Session Affinity für `/blazor`.

### Fund #4: OpenID-Connect-Challenge auf Komponenten-/Circuit-Endpoints bricht mit 500

- `curl` gegen `http://localhost:5004/viewer?...` und `/_blazor` ohne Session-Cookie: 500 statt Challenge, weil die OpenIdConnect-Challenge über PAR gegen Keycloak läuft und Keycloak `invalid_request: Invalid parameter: redirect_uri` antwortet.
- Für Razor Pages war dieses Verhalten vermutlich identisch (vorhandene Keycloak-Client-Konfiguration), aber jetzt betrifft es zusätzlich `/_blazor`: ein Circuit-Request ohne Cookie/Session wird nicht mit 401, sondern mit 500 beantwortet (Circuit-Transport kann einer 302-Challenge nicht folgen).
- Bestätigt im Dev-Container: `POST /_blazor/negotiate` 500 und `POST /_blazor/disconnect` 500 mit `OpenIdConnectProtocolException: invalid_request: Invalid parameter: redirect_uri` (Keycloak-PAR lehnt die Challenge ab, Client `ballwareweb` hat die Service-Redirect-URI nicht registriert).
- Auslöser nach jedem Container-Neustart: `AddSession()` läuft gegen `AddDistributedMemoryCache()` (Redis-Block in `Startup.cs` auskommentiert) → Sessions sind In-Process und mit dem Container weg; gleicher Effekt für den Data-Protection-Keyring. Ein im Browser offener iframe mit alter Session probiert danach reconnecten und produziert diese 500er, bis das Frontend erneut `/signon/{idToken}` aufruft.

### Laufzeitbefund Viewer im iframe (in Arbeit)

- `DX-DIAG Viewer …` zeigt korrekten Aufruf `viewer?docId=…&id=…` mit `reportName='docId=…&id=…'`, einmal im Prerender und einmal im Circuit (nach `/_blazor/negotiate 200`) → Routing, Query-Übergabe und Circuit-Auth funktionieren.
- Aber: **kein `DX-DIAG GetData`** → DX fordert serverseitig kein Report-Modell an, der Viewer bleibt beim `dx-skeleton-screen-viewer`. Die fehlende Kette liegt also clientseitig (DX-Viewer-Initialisierung/Modellanforderung), nicht an Routing, Storage-Regex oder Tenantauflösung.
- Nebenbefund: `customization.js` ruft aktuell die von DX dokumentierten drei Dateien auf (`dx-reporting`, `dx-analytics-core`, `dx-rich`); ein `dx-querybuilder.de.json` existiert nicht (DX liefert dazu nur `dx-querybuilder.min.js`) und wird auch nicht angefragt.

### Noch offen (Staging/UAT)

- Iframe-UAT aus dem echten Frontend: SignOn-Redirect, Speichern/Laden von Berichten, leerer Bericht (`new`), Berechtigungsmatrix (DataSource-UI), deutsch lokalisierte UI + ausgeblendete Menüpunkte (`dxrd-newreport`, `dxrd-newreport-via-wizard`, `dxrd-run-wizard`, `dxrd-exit`), Viewer-Parameter/Print/Export, Custom-Functions, Mail-Job-Reportgenerierung, unauthentifizierter Zugriff (Challenge wie bisher).
- SignalR-Circuit-Auth im iframe-Setup und Reverse-Proxy: WebSocket-Upgrade für `/blazor` (+ SameSite=None-Cookie) konfigurieren/testen; `DocumentStorage`-Tenantauflösung über `IHttpContextAccessor` innerhalb der DX-Requests explizit prüfen.
- Release-Image-Start + Health (WP4.2) braucht `GITHUB_PAT`/`DEVEXPRESS_NUGETFEED` in der CI- Umgebung.
- Hinweis: `.gitignore` ignoriert jetzt `ballware-document-service.env` / `*.local.env` (enthielt Zugangsdaten).
