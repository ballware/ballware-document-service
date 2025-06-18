using System.Globalization;
using Ballware.Document.Authorization;
using Ballware.Document.Engine.Dx;
using Ballware.Document.Jobs;
using Ballware.Document.Metadata;
using Ballware.Document.Service.Adapter;
using Ballware.Document.Service.Configuration;
using Ballware.Document.Service.Mappings;
using Ballware.Document.Session;
using Ballware.Generic.Client;
using Ballware.Meta.Client;
using Ballware.Storage.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using Quartz;
using CorsOptions = Ballware.Document.Service.Configuration.CorsOptions;
using SwaggerOptions = Ballware.Document.Service.Configuration.SwaggerOptions;
using Serilog;
using SessionOptions = Ballware.Document.Session.Configuration.SessionOptions;

namespace Ballware.Document.Service;

public class Startup(IWebHostEnvironment environment, ConfigurationManager configuration, IServiceCollection services)
{
    private IWebHostEnvironment Environment { get; } = environment;
    private ConfigurationManager Configuration { get; } = configuration;
    private IServiceCollection Services { get; } = services;

    public void InitializeServices()
    {   
        CorsOptions? corsOptions = Configuration.GetSection("Cors").Get<CorsOptions>();
        AuthorizationOptions? authorizationOptions =
            Configuration.GetSection("Authorization").Get<AuthorizationOptions>();
        SessionOptions? sessionOptions = Configuration.GetSection("Session").Get<SessionOptions>();
        SwaggerOptions? swaggerOptions = Configuration.GetSection("Swagger").Get<SwaggerOptions>();
        ServiceClientOptions? metaClientOptions = Configuration.GetSection("MetaClient").Get<ServiceClientOptions>();
        ServiceClientOptions? storageClientOptions = Configuration.GetSection("StorageClient").Get<ServiceClientOptions>();
        ServiceClientOptions? genericClientOptions = Configuration.GetSection("GenericClient").Get<ServiceClientOptions>();
        
        Services.AddOptionsWithValidateOnStart<AuthorizationOptions>()
            .Bind(Configuration.GetSection("Authorization"))
            .ValidateDataAnnotations();
        
        Services.AddOptionsWithValidateOnStart<SessionOptions>()
            .Bind(Configuration.GetSection("Session"))
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

        if (authorizationOptions == null)
        {
            throw new ConfigurationException("Required configuration for authorization is missing");
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
            .AddJwtBearer(options =>
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
        Services.AddBallwareDocumentAuthorizationUtils(authorizationOptions.TenantClaim, authorizationOptions.UserIdClaim, authorizationOptions.RightClaim);
        
        Services.AddHttpContextAccessor();
        
        Services.AddMvcCore()
            .AddApiExplorer();

        Services.AddControllers();
        
        Services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));
        Services.AddBallwareDocumentBackgroundJobs();
        
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
            });
        
        Services.AddHttpClient<BallwareMetaClient>(client =>
            {
                client.BaseAddress = new Uri(metaClientOptions.ServiceUrl);
            })
#if DEBUG            
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
#endif                  
            .AddClientCredentialsTokenHandler("meta");

        Services.AddHttpClient<BallwareStorageClient>(client =>
            {
                client.BaseAddress = new Uri(storageClientOptions.ServiceUrl);
            })
#if DEBUG            
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
#endif            
            .AddClientCredentialsTokenHandler("storage");
        
        Services.AddHttpClient<BallwareGenericClient>(client =>
            {
                client.BaseAddress = new Uri(genericClientOptions.ServiceUrl);
            })
#if DEBUG            
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
#endif                        
            .AddClientCredentialsTokenHandler("generic");
        
        Services.AddAutoMapper(config =>
        {
            config.AddProfile<MetaServiceDocumentMetadataProfile>();
            config.AddProfile<GenericServiceDocumentMetadataProfile>();
        });
        
        Services.AddEndpointsApiExplorer();

        Services.AddScoped<IDocumentMetadataProvider, MetaServiceDocumentMetadataProvider>();
        Services.AddScoped<IMetaDatasourceProvider, MetaServiceDatasourceProvider>();
        Services.AddScoped<ITenantDatasourceProvider, GenericServiceDatasourceProvider>();
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