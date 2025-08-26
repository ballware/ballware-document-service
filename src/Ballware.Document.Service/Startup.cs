using System.Globalization;
using Ballware.Document.Api;
using Ballware.Document.Api.Endpoints;
using Ballware.Document.Data.Ef;
using Ballware.Document.Data.Ef.Configuration;
using Ballware.Document.Data.Ef.Postgres;
using Ballware.Document.Data.Ef.SqlServer;
using Ballware.Shared.Authorization;
using Ballware.Document.Engine.Dx;
using Ballware.Document.Jobs;
using Ballware.Document.Jobs.Configuration;
using Ballware.Document.Metadata;
using Ballware.Document.Service.Adapter;
using Ballware.Document.Service.Configuration;
using Ballware.Document.Service.Endpoints;
using Ballware.Document.Service.Extensions;
using Ballware.Document.Service.Mappings;
using Ballware.Document.Session;
using Ballware.Generic.Service.Client;
using Ballware.Meta.Service.Client;
using Ballware.Ml.Service.Client;
using Ballware.Shared.Api;
using Ballware.Shared.Api.Endpoints;
using Ballware.Shared.Authorization.Jint;
using Ballware.Shared.Data.Repository;
using Ballware.Storage.Service.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi.Models;
using Quartz;
using CorsOptions = Ballware.Document.Service.Configuration.CorsOptions;
using SwaggerOptions = Ballware.Document.Service.Configuration.SwaggerOptions;
using Serilog;
using SessionOptions = Ballware.Document.Session.Configuration.SessionOptions;

namespace Ballware.Document.Service;

public class Startup(IWebHostEnvironment environment, ConfigurationManager configuration, IServiceCollection services)
{
    private const string ClaimTypeScope = "scope";
    
    private IWebHostEnvironment Environment { get; } = environment;
    private ConfigurationManager Configuration { get; } = configuration;
    private IServiceCollection Services { get; } = services;

    public void InitializeServices()
    {   
        CorsOptions? corsOptions = Configuration.GetSection("Cors").Get<CorsOptions>();
        AuthorizationOptions? authorizationOptions =
            Configuration.GetSection("Authorization").Get<AuthorizationOptions>();
        StorageOptions? storageOptions = Configuration.GetSection("Storage").Get<StorageOptions>();
        SessionOptions? sessionOptions = Configuration.GetSection("Session").Get<SessionOptions>();
        MailOptions? mailOptions = Configuration.GetSection("Mail").Get<MailOptions>();
        TriggerOptions? triggerOptions = Configuration.GetSection("Trigger").Get<TriggerOptions>();
        SwaggerOptions? swaggerOptions = Configuration.GetSection("Swagger").Get<SwaggerOptions>();
        ServiceClientOptions? metaClientOptions = Configuration.GetSection("MetaClient").Get<ServiceClientOptions>();
        ServiceClientOptions? storageClientOptions = Configuration.GetSection("StorageClient").Get<ServiceClientOptions>();
        ServiceClientOptions? genericClientOptions = Configuration.GetSection("GenericClient").Get<ServiceClientOptions>();
        ServiceClientOptions? mlClientOptions = Configuration.GetSection("MlClient").Get<ServiceClientOptions>();
        
        Services.AddOptionsWithValidateOnStart<AuthorizationOptions>()
            .Bind(Configuration.GetSection("Authorization"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<StorageOptions>()
            .Bind(Configuration.GetSection("Storage"))
            .ValidateDataAnnotations();            
        
        Services.AddOptionsWithValidateOnStart<SessionOptions>()
            .Bind(Configuration.GetSection("Session"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<TriggerOptions>()
            .Bind(Configuration.GetSection("Trigger"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<MailOptions>()
            .Bind(Configuration.GetSection("Mail"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<SwaggerOptions>()
            .Bind(Configuration.GetSection("Swagger"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<ServiceClientOptions>()
            .Bind(Configuration.GetSection("MetaClient"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<ServiceClientOptions>()
            .Bind(Configuration.GetSection("StorageClient"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<ServiceClientOptions>()
            .Bind(Configuration.GetSection("GenericClient"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<ServiceClientOptions>()
            .Bind(Configuration.GetSection("MlClient"))
            .ValidateDataAnnotations();

        if (authorizationOptions == null || storageOptions == null)
        {
            throw new ConfigurationException("Required configuration for authorization or storage is missing");
        }
        
        var validProviders = new[] { "mssql", "postgres" };
        
        if (!validProviders.Contains(storageOptions.Provider))
        {
            throw new ConfigurationException("Invalid storage provider specified. Valid providers are: " + string.Join(", ", validProviders));
        }
        
        if (mailOptions == null)
        {
            throw new ConfigurationException("Required configuration for mail is missing");
        }

        if (triggerOptions == null)
        {
            triggerOptions = new TriggerOptions();
        }
        
        if (sessionOptions == null)
        {
            sessionOptions = new SessionOptions();
        }
        
        if (metaClientOptions == null)
        {
            throw new ConfigurationException("Required configuration for metaClient is missing");
        }

        if (storageClientOptions == null)
        {
            throw new ConfigurationException("Required configuration for storageClient is missing");
        }
        
        if (genericClientOptions == null)
        {
            throw new ConfigurationException("Required configuration for genericClient is missing");
        }
        
        if (mlClientOptions == null)
        {
            throw new ConfigurationException("Required configuration for mlClient is missing");
        }

        services.Configure<FormOptions>(x =>
        {
            x.ValueLengthLimit = int.MaxValue;
        });
        
        Services.AddMemoryCache();
        Services.AddDistributedMemoryCache();
        
        Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authorizationOptions.Authority;
                options.ClientId = authorizationOptions.ClientId;
                options.ClientSecret = null;
                options.ResponseType = "code";
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add(authorizationOptions.RequiredUserScope);
                
                options.RegisterBallwareSessionTokenHandling();
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.Authority = authorizationOptions.Authority;
                options.Audience = authorizationOptions.Audience;
                options.RequireHttpsMetadata = authorizationOptions.RequireHttpsMetadata;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    ValidIssuer = authorizationOptions.Authority
                };
            });

        Services.AddSingleton<ConfigurationManager<OpenIdConnectConfiguration>>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                .Get(OpenIdConnectDefaults.AuthenticationScheme);

            return new ConfigurationManager<OpenIdConnectConfiguration>(
                options.Authority!.TrimEnd('/') + "/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());
        });

        Services.AddAuthorizationBuilder()
            .AddPolicy("documentApi", policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User
                        .Claims
                        .Where(c => ClaimTypeScope == c.Type)
                        .SelectMany(c => c.Value.Split(' '))
                        .Any(s => s.Equals(authorizationOptions.RequiredUserScope, StringComparison.Ordinal)))
            );
        
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
        
        Services.AddBallwareSession(sessionOptions);
        Services.AddBallwareSharedAuthorizationUtils(authorizationOptions.TenantClaim, authorizationOptions.UserIdClaim, authorizationOptions.RightClaim);
        
        Services.AddHttpContextAccessor();
        
        Services.AddMvcCore()
            .AddApiExplorer();

        Services.AddControllers();
        
        Services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));
        Services.AddBallwareDocumentBackgroundJobs(mailOptions, triggerOptions);
        
        Services.AddClientCredentialsTokenManagement()
            .AddClient("meta", client =>
            {
                client.TokenEndpoint = metaClientOptions.TokenEndpoint;

                client.ClientId = metaClientOptions.ClientId;
                client.ClientSecret = metaClientOptions.ClientSecret;

                client.Scope = metaClientOptions.Scopes;
            })
            .AddClient("storage", client =>
            {
                client.TokenEndpoint = storageClientOptions.TokenEndpoint;

                client.ClientId = storageClientOptions.ClientId;
                client.ClientSecret = storageClientOptions.ClientSecret;

                client.Scope = storageClientOptions.Scopes;
            })
            .AddClient("generic", client =>
            {
                client.TokenEndpoint = genericClientOptions.TokenEndpoint;

                client.ClientId = genericClientOptions.ClientId;
                client.ClientSecret = genericClientOptions.ClientSecret;

                client.Scope = genericClientOptions.Scopes;
            })
            .AddClient("ml", client =>
            {
                client.TokenEndpoint = mlClientOptions.TokenEndpoint;

                client.ClientId = mlClientOptions.ClientId;
                client.ClientSecret = mlClientOptions.ClientSecret;

                client.Scope = mlClientOptions.Scopes;
            });
        
        Services.AddHttpClient<MetaServiceClient>(client =>
            {
                client.BaseAddress = new Uri(metaClientOptions.ServiceUrl);
            })
#if DEBUG     
// SonarQube: Disable S4830 - Accepting any server certificate is intended here for debug purposes
#pragma warning disable S4830      
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
#pragma warning restore S4830            
#endif                  
            .AddClientCredentialsTokenHandler("meta");

        Services.AddHttpClient<StorageServiceClient>(client =>
            {
                client.BaseAddress = new Uri(storageClientOptions.ServiceUrl);
            })
#if DEBUG     
// SonarQube: Disable S4830 - Accepting any server certificate is intended here for debug purposes
#pragma warning disable S4830
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                // SonarQube: Disable S4830 - Accepting any server certificate is intended here

                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
#pragma warning restore S4830            
#endif            
            .AddClientCredentialsTokenHandler("storage");
        
        Services.AddHttpClient<GenericServiceClient>(client =>
            {
                client.BaseAddress = new Uri(genericClientOptions.ServiceUrl);
            })
#if DEBUG     
// SonarQube: Disable S4830 - Accepting any server certificate is intended here for debug purposes
#pragma warning disable S4830
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
#pragma warning restore S4830             
#endif                        
            .AddClientCredentialsTokenHandler("generic");
        
        Services.AddHttpClient<MlServiceClient>(client =>
            {
                client.BaseAddress = new Uri(mlClientOptions.ServiceUrl);
            })
#if DEBUG     
// SonarQube: Disable S4830 - Accepting any server certificate is intended here for debug purposes
#pragma warning disable S4830
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
#pragma warning restore S4830             
#endif                        
            .AddClientCredentialsTokenHandler("ml");
        
        Services.AddAutoMapper(config =>
        {
            config.AddBallwareDocumentStorageMappings();
            config.AddProfile<MetaServiceDocumentMetadataProfile>();
            config.AddProfile<GenericServiceDocumentMetadataProfile>();
            config.AddProfile<MlServiceDocumentMetadataProfile>();
        });
        
        var storageConnectionStringIdentifier = storageOptions.ConnectionString;
            
        if (string.IsNullOrWhiteSpace(storageConnectionStringIdentifier))
        {
            throw new ConfigurationException("Storage connection string is not configured");
        }
            
        var storageConnectionString = Configuration.GetConnectionString(storageConnectionStringIdentifier);
            
        if (string.IsNullOrWhiteSpace(storageConnectionString))
        {
            throw new ConfigurationException("Storage connection string is not found in configuration");
        }
        
        if ("mssql".Equals(storageOptions.Provider, StringComparison.InvariantCultureIgnoreCase))
        {
            Services.AddBallwareDocumentStorageForSqlServer(storageOptions, storageConnectionString);    
        } 
        else if ("postgres".Equals(storageOptions.Provider, StringComparison.InvariantCultureIgnoreCase))
        {
            Services.AddBallwareDocumentStorageForPostgres(storageOptions, storageConnectionString);
        }
        
        Services.AddEndpointsApiExplorer();

        Services.AddScoped<IDocumentMetadataProvider, DocumentMetadataProvider>();
        Services.AddScoped<INotificationMetadataProvider, NotificationMetadataProvider>();
        Services.AddScoped<ISubscriptionMetadataProvider, SubscriptionMetadataProvider>();
        Services.AddScoped<IDocumentProcessingStateProvider, MetaServiceProvider>();
        Services.AddScoped<IProcessingStateProvider, MetaServiceProvider>();
        Services.AddScoped<IDocumentPickvalueProvider, MetaServiceProvider>();
        Services.AddScoped<IMetaDatasourceProvider, MetaServiceProvider>();
        Services.AddScoped<IDocumentLookupProvider, GenericServiceProvider>();
        Services.AddScoped<ITenantableRepositoryHook<Data.Public.Document, Data.Persistables.Document>, DocumentImportExportRepositoryHook>();
        Services.AddScoped<IAuthorizationMetadataProvider, MetaServiceProvider>();
        Services.AddScoped<IFileStorageProvider, StorageServiceProvider>();
        Services.AddScoped<IJobMetadataProvider, MetaServiceProvider>();
        Services.AddScoped<IExportMetadataProvider, MetaServiceProvider>();
        
        Services.AddScoped<IDatasourceDefinitionProvider, GenericServiceProvider>();
        Services.AddScoped<IDatasourceDefinitionProvider, MetaServiceProvider>();
        Services.AddScoped<IDatasourceDefinitionProvider, MlServiceProvider>();

        Services.AddBallwareSharedJintRightsChecker();
        Services.AddBallwareSharedApiDependencies();
        
        Services.AddBallwareDevExpressReporting();
        
        if (swaggerOptions != null)
        {
            Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("document", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "ballware document user API",
                    Version = "v1"
                });
                
                c.SwaggerDoc("service", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "ballware document service API",
                    Version = "v1"
                });
                
                c.EnableAnnotations();

                c.AddSecurityDefinition("oidc", new OpenApiSecurityScheme
                {
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.OpenIdConnect,
                    OpenIdConnectUrl = new Uri(authorizationOptions.Authority + "/.well-known/openid-configuration")
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oidc" }
                        },
                        swaggerOptions.RequiredScopes.Split(" ")
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

        app.UseBallwareSession();
        
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStaticFiles();
        
        app.Use(async (context, next) =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            await next();
        });
        
        app.UseBallwareDevExpressReporting();

        app.MapControllers();
        app.MapRazorPages();
        app.MapSignOnEndpoint();
        
        app.MapDocumentUserApi("/document/document");
        app.MapDocumentServiceApi("/document/document");
        app.MapTenantableEditingApi<Data.Public.Document>("/document/document", "meta", "document", "Document", "Document", "documentApi", "document");
        
        app.MapNotificationUserApi("/document/notification");
        app.MapNotificationServiceApi("/document/notification");
        app.MapTenantableEditingApi<Data.Public.Notification>("/document/notification", "meta", "notification", "Notification", "Notification", "documentApi", "document");
        
        app.MapSubscriptionUserApi("/document/subscription");
        app.MapSubscriptionServiceApi("/document/subscription");
        app.MapTenantableEditingApi<Data.Public.Subscription>("/document/subscription", "meta", "subscription", "Subscription", "Subscription", "documentApi", "document");
        
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