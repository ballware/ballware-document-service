using System.Globalization;
using Ballware.Document.App.Components;
using Ballware.Document.App.Middleware;
using Ballware.Document.Engine.Dx;
using Ballware.Document.Hosting;
using Ballware.Document.Hosting.Configuration;
using Ballware.Document.Session;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Serilog;

namespace Ballware.Document.App;

public class Startup(IWebHostEnvironment environment, ConfigurationManager configuration, IServiceCollection services)
{
    private IWebHostEnvironment Environment { get; } = environment;
    private ConfigurationManager Configuration { get; } = configuration;
    private IServiceCollection Services { get; } = services;

    private string FrameAncestorsPolicy { get; } = ResolveFrameAncestorsPolicy(configuration);

    private static string ResolveFrameAncestorsPolicy(ConfigurationManager configuration)
    {
        var configured = configuration["ContentSecurity:FrameAncestors"];

        if (string.IsNullOrWhiteSpace(configured))
        {
            return "'self'";
        }

        return NormalizeFrameAncestorsPolicy(configured.Trim());
    }

    private static string NormalizeFrameAncestorsPolicy(string policy)
    {
        if (string.Equals(policy, "self", StringComparison.OrdinalIgnoreCase))
        {
            return "'self'";
        }

        if (string.Equals(policy, "none", StringComparison.OrdinalIgnoreCase))
        {
            return "'none'";
        }

        if (policy.StartsWith("self ", StringComparison.OrdinalIgnoreCase))
        {
            return "'self' " + policy.Substring(5);
        }

        return policy;
    }

    private bool FramingFromForeignOriginsAllowed =>
        !string.Equals(FrameAncestorsPolicy, "'self'", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(FrameAncestorsPolicy, "'none'", StringComparison.OrdinalIgnoreCase);

    public void InitializeServices()
    {
        BallwareDocumentOptions documentOptions = Services.AddBallwareDocumentOptions(Configuration);

        Services.AddBallwareDocumentAuthentication(documentOptions, withSession: true, "ballware-document-app", Configuration["DataProtection:KeysPath"]);

        var detailedErrors = Configuration.GetValue<bool?>("DetailedErrors") ?? Environment.IsDevelopment();

        Services.AddRazorComponents()
            .AddInteractiveServerComponents(options => options.DetailedErrors = detailedErrors)
            .AddHubOptions(options => options.MaximumReceiveMessageSize = 256 * 1024);

        Services.Configure<AntiforgeryOptions>(options =>
        {
            // Gerahmt wird über die CSP frame-ancestors-Richtlinie der Razor-Komponenten-Endpoints.
            // Nur wenn frame-ancestors 'self' oder 'none' ist (kein externes Framing erlaubt),
            // bleibt das "X-Frame-Options: SAMEORIGIN" des Antiforgery-Middleware als Fallback aktiv.
            options.SuppressXFrameOptionsHeader = FramingFromForeignOriginsAllowed;
            options.Cookie.Name = ".Ballware.Antiforgery";
        });

        Services.AddBallwareDocumentServiceClients(documentOptions);

        Services.AddBallwareDocumentDataStore(documentOptions.Storage, Configuration);

        Services.AddBallwareDocumentMetadataProviders();

        // AddDevExpressBlazorReporting benötigt MVC-Core-Dienste (IUrlHelperFactory, ApplicationPartManager).
        Services.AddMvcCore();

        Services.AddBallwareDevExpressReportingBlazor();
    }

    public void InitializeApp(WebApplication app)
    {
        var authorizationOptions = app.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        if (string.IsNullOrWhiteSpace(authorizationOptions.PublicBaseUrl))
        {
            Log.Warning(
                "Authorization:PublicBaseUrl is not configured. The OIDC sign-in redirect_uri is derived from the incoming request ({ClientId}), so every host the app is reached with must be registered as a valid redirect URI for its callback path at the identity provider.",
                authorizationOptions.ClientId);
        }

        if (Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            IdentityModelEventSource.ShowPII = true;
        }

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionFeature?.Error;

                if (exception != null)
                {
                    Log.Error(exception, "Unhandled exception occurred");

                    var problemDetails = new ProblemDetails
                    {
                        Type = "https://httpstatuses.com/500",
                        Title = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = app.Environment.IsDevelopment() ? exception.ToString() : null,
                        Instance = context.Request.Path
                    };

                    context.Response.StatusCode = problemDetails.Status.Value;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(problemDetails);
                }
            });
        });

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseRouting();

        app.UseBallwareSession();

        app.UseMiddleware<ChallengeDifferentiationMiddleware>();

        app.UseAuthentication();

        // muss die Autorisierungsmiddleware umschließen, deren Challenge die IdP-Fehler auslöst
        app.UseMiddleware<SignInChallengeFallbackMiddleware>();

        app.UseAuthorization();
        app.UseAntiforgery();
        app.UseStaticFiles();

        app.Use(async (context, next) =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            await next();
        });

        app.UseBallwareDevExpressReportingBlazor();

        app.MapBallwareDocumentSignOnEndpoint();

        app.MapRazorComponents<Ballware.Document.App.Components.App>()
            .AddAdditionalAssemblies(typeof(Ballware.Document.Engine.Dx.Components.Designer).Assembly)
            .AddInteractiveServerRenderMode(options =>
            {
                options.ContentSecurityFrameAncestorsPolicy = FrameAncestorsPolicy;
            });
    }
}
