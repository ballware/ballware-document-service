using System.Globalization;
using Ballware.Document.Api;
using Ballware.Document.Api.Endpoints;
using Ballware.Document.Engine.Dx;
using Ballware.Document.Hosting;
using Ballware.Document.Hosting.Adapter;
using Ballware.Document.Hosting.Configuration;
using Ballware.Document.Jobs;
using Ballware.Document.Jobs.Configuration;
using Ballware.Document.Metadata;
using Ballware.Document.Service.Extensions;
using Ballware.Shared.Api;
using Ballware.Shared.Api.Endpoints;
using Ballware.Shared.Authorization.Jint;
using Ballware.Shared.Data.Repository;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi;
using Quartz;
using Serilog;

namespace Ballware.Document.Service;

public class Startup(IWebHostEnvironment environment, ConfigurationManager configuration, IServiceCollection services)
{
    private IWebHostEnvironment Environment { get; } = environment;
    private ConfigurationManager Configuration { get; } = configuration;
    private IServiceCollection Services { get; } = services;

    public void InitializeServices()
    {   
        CorsOptions? corsOptions = Configuration.GetSection("Cors").Get<CorsOptions>();
        MailOptions? mailOptions = Configuration.GetSection("Mail").Get<MailOptions>();
        TriggerOptions? triggerOptions = Configuration.GetSection("Trigger").Get<TriggerOptions>();
        SwaggerOptions? swaggerOptions = Configuration.GetSection("Swagger").Get<SwaggerOptions>();

        Services.AddOptionsWithValidateOnStart<TriggerOptions>()
            .Bind(Configuration.GetSection("Trigger"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<MailOptions>()
            .Bind(Configuration.GetSection("Mail"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<SwaggerOptions>()
            .Bind(Configuration.GetSection("Swagger"))
            .ValidateDataAnnotations();

        BallwareDocumentOptions documentOptions = Services.AddBallwareDocumentOptions(Configuration);

        if (mailOptions == null)
        {
            throw new ConfigurationException("Required configuration for mail is missing");
        }

        if (triggerOptions == null)
        {
            triggerOptions = new TriggerOptions();
        }

        services.Configure<FormOptions>(x =>
        {
            x.ValueLengthLimit = int.MaxValue;
        });
        
        Services.AddBallwareDocumentAuthentication(documentOptions, withSession: false, "ballware-document-service", Configuration["DataProtection:KeysPath"]);

        if (corsOptions != null)
        {
            Services.AddCors(options =>
            {
                options.AddDefaultPolicy(c =>
                {
                    c.WithOrigins(corsOptions.AllowedOrigins)
                        .WithMethods(corsOptions.AllowedMethods)
                        .WithHeaders(corsOptions.AllowedHeaders);
                });
            });
        }
        
        Services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
        
        Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null;
        });
        
        Services.AddMvcCore()
            .AddApiExplorer();

        Services.AddControllers();
        
        Services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));
        Services.AddBallwareDocumentBackgroundJobs(mailOptions, triggerOptions);
        
        Services.AddBallwareDocumentServiceClients(documentOptions);
        
        Services.AddBallwareDocumentDataStore(documentOptions.Storage, Configuration);
        
        Services.AddEndpointsApiExplorer();

        Services.AddBallwareDocumentMetadataProviders();

        Services.AddScoped<INotificationMetadataProvider, NotificationMetadataProvider>();
        Services.AddScoped<ISubscriptionMetadataProvider, SubscriptionMetadataProvider>();
        Services.AddScoped<IProcessingStateProvider, MetaServiceProvider>();
        Services.AddScoped<ITenantableRepositoryHook<Data.Public.Document, Data.Persistables.Document>, DocumentImportExportRepositoryHook>();
        Services.AddScoped<IFileStorageProvider, StorageServiceProvider>();
        Services.AddScoped<IJobMetadataProvider, MetaServiceProvider>();
        Services.AddScoped<IExportMetadataProvider, MetaServiceProvider>();
        
        Services.AddBallwareSharedJintRightsChecker();
        Services.AddBallwareSharedApiDependencies();
        
        Services.AddBallwareDevExpressReportingCore();
        
        if (swaggerOptions != null)
        {
            Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("document", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "ballware document user API",
                    Version = "v1"
                });
                
                c.SwaggerDoc("service", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "ballware document service API",
                    Version = "v1"
                });
                
                c.EnableAnnotations();

                c.AddSecurityDefinition("oidc", new OpenApiSecurityScheme
                {
                    Type = Microsoft.OpenApi.SecuritySchemeType.OpenIdConnect,
                    OpenIdConnectUrl = new Uri(documentOptions.Authorization.Authority + "/.well-known/openid-configuration")
                });

                c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("oidc"),
                        swaggerOptions.RequiredScopes.Split(" ").ToList()
                    }
                });
            });
        }
    }

    public void InitializeApp(WebApplication app)
    {
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

        app.UseCors();
        
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseRouting();
        
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStaticFiles();
        
        app.Use(async (context, next) =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            await next();
        });
        
        app.UseBallwareDevExpressReportingCore();

        app.MapControllers();
        
        app.MapDocumentUserApi("/document/document");
        app.MapDocumentServiceApi("/document/document");
        app.MapTenantableEditingApi<Data.Public.Document>("/document/document", "meta", "document", "Document", "Document", "documentApi", "document");
        
        app.MapNotificationUserApi("/document/notification");
        app.MapNotificationServiceApi("/document/notification");
        app.MapTenantableEditingApi<Data.Public.Notification>("/document/notification", "meta", "notification", "Notification", "Notification", "documentApi", "document");
        
        app.MapSubscriptionUserApi("/document/subscription");
        app.MapSubscriptionServiceApi("/document/subscription");
        app.MapTenantableEditingApi<Data.Public.Subscription>("/document/subscription", "meta", "subscription", "Subscription", "Subscription", "documentApi", "document");
        
        app.MapProcessingStateDocumentApi("/document/processingstate");

        var authorizationOptions = app.Services.GetService<IOptions<AuthorizationOptions>>()?.Value;
        var swaggerOptions = app.Services.GetService<IOptions<SwaggerOptions>>()?.Value;

        if (swaggerOptions != null && authorizationOptions != null)
        {
            app.MapSwagger();

            app.UseSwagger();

            if (swaggerOptions.EnableClient)
            {
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("document/swagger.json", "ballware document user API");
                    c.SwaggerEndpoint("service/swagger.json", "ballware document service API");

                    c.OAuthClientId(swaggerOptions.ClientId);
                    c.OAuthClientSecret(swaggerOptions.ClientSecret);
                    c.OAuthScopes(swaggerOptions.RequiredScopes?.Split(" "));
                    c.OAuthUsePkce();
                });
            }
        }
    }
}