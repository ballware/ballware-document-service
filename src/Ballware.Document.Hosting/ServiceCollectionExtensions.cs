using Ballware.Document.Data.Ef;
using Ballware.Document.Data.Ef.Configuration;
using Ballware.Document.Data.Ef.Postgres;
using Ballware.Document.Data.Ef.SqlServer;
using Ballware.Document.Data.Repository;
using Ballware.Document.Hosting.Adapter;
using Ballware.Document.Hosting.Configuration;
using Ballware.Document.Hosting.Mappings;
using Ballware.Document.Metadata;
using Ballware.Document.Session;
using Ballware.Document.Session.Configuration;
using Ballware.Generic.Service.Client;
using Ballware.Meta.Service.Client;
using Ballware.Ml.Service.Client;
using Ballware.Shared.Authorization;
using Ballware.Storage.Service.Client;
using Duende.AccessTokenManagement;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Ballware.Document.Hosting;

public static class ServiceCollectionExtensions
{
    private const string ClaimTypeScope = "scope";
    private const string ServiceApiScope = "serviceApi";

    public static BallwareDocumentOptions AddBallwareDocumentOptions(this IServiceCollection services, IConfiguration configuration)
    {
        AuthorizationOptions? authorizationOptions =
            configuration.GetSection("Authorization").Get<AuthorizationOptions>();
        StorageOptions? storageOptions = configuration.GetSection("Storage").Get<StorageOptions>();
        SessionOptions? sessionOptions = configuration.GetSection("Session").Get<SessionOptions>();
        ServiceClientOptions? metaClientOptions = configuration.GetSection("MetaClient").Get<ServiceClientOptions>();
        ServiceClientOptions? storageClientOptions = configuration.GetSection("StorageClient").Get<ServiceClientOptions>();
        ServiceClientOptions? genericClientOptions = configuration.GetSection("GenericClient").Get<ServiceClientOptions>();
        ServiceClientOptions? mlClientOptions = configuration.GetSection("MlClient").Get<ServiceClientOptions>();

        services.AddOptionsWithValidateOnStart<AuthorizationOptions>()
            .Bind(configuration.GetSection("Authorization"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<StorageOptions>()
            .Bind(configuration.GetSection("Storage"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<SessionOptions>()
            .Bind(configuration.GetSection("Session"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<ServiceClientOptions>()
            .Bind(configuration.GetSection("MetaClient"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<ServiceClientOptions>()
            .Bind(configuration.GetSection("StorageClient"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<ServiceClientOptions>()
            .Bind(configuration.GetSection("GenericClient"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<ServiceClientOptions>()
            .Bind(configuration.GetSection("MlClient"))
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

        return new BallwareDocumentOptions(
            authorizationOptions,
            storageOptions,
            sessionOptions,
            metaClientOptions,
            storageClientOptions,
            genericClientOptions,
            mlClientOptions);
    }

    public static IServiceCollection AddBallwareDocumentServiceClients(this IServiceCollection services, BallwareDocumentOptions options)
    {
        services.AddClientCredentialsTokenManagement()
            .AddClient("meta", client =>
            {
                client.TokenEndpoint = new Uri(options.MetaClient.TokenEndpoint);

                client.ClientId = ClientId.Parse(options.MetaClient.ClientId);
                client.ClientSecret = ClientSecret.Parse(options.MetaClient.ClientSecret);

                client.Scope = Duende.AccessTokenManagement.Scope.Parse(options.MetaClient.Scopes);
            })
            .AddClient("storage", client =>
            {
                client.TokenEndpoint = new Uri(options.StorageClient.TokenEndpoint);

                client.ClientId = ClientId.Parse(options.StorageClient.ClientId);
                client.ClientSecret = ClientSecret.Parse(options.StorageClient.ClientSecret);

                client.Scope = Duende.AccessTokenManagement.Scope.Parse(options.StorageClient.Scopes);
            })
            .AddClient("generic", client =>
            {
                client.TokenEndpoint = new Uri(options.GenericClient.TokenEndpoint);

                client.ClientId = ClientId.Parse(options.GenericClient.ClientId);
                client.ClientSecret = ClientSecret.Parse(options.GenericClient.ClientSecret);

                client.Scope = Duende.AccessTokenManagement.Scope.Parse(options.GenericClient.Scopes);
            })
            .AddClient("ml", client =>
            {
                client.TokenEndpoint = new Uri(options.MlClient.TokenEndpoint);

                client.ClientId = ClientId.Parse(options.MlClient.ClientId);
                client.ClientSecret = ClientSecret.Parse(options.MlClient.ClientSecret);

                client.Scope = Duende.AccessTokenManagement.Scope.Parse(options.MlClient.Scopes);
            });

        services.AddHttpClient<MetaServiceClient>(client =>
            {
                client.BaseAddress = new Uri(options.MetaClient.ServiceUrl);
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
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("meta"));

        services.AddHttpClient<StorageServiceClient>(client =>
            {
                client.BaseAddress = new Uri(options.StorageClient.ServiceUrl);
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
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("storage"));

        services.AddHttpClient<GenericServiceClient>(client =>
            {
                client.BaseAddress = new Uri(options.GenericClient.ServiceUrl);
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
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("generic"));

        services.AddHttpClient<MlServiceClient>(client =>
            {
                client.BaseAddress = new Uri(options.MlClient.ServiceUrl);
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
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("ml"));

        return services;
    }

    public static IServiceCollection AddBallwareDocumentDataStore(this IServiceCollection services, StorageOptions storageOptions, IConfiguration configuration)
    {
        var storageConnectionStringIdentifier = storageOptions.ConnectionString;

        if (string.IsNullOrWhiteSpace(storageConnectionStringIdentifier))
        {
            throw new ConfigurationException("Storage connection string is not configured");
        }

        var storageConnectionString = configuration.GetConnectionString(storageConnectionStringIdentifier);

        if (string.IsNullOrWhiteSpace(storageConnectionString))
        {
            throw new ConfigurationException("Storage connection string is not found in configuration");
        }

        if ("mssql".Equals(storageOptions.Provider, StringComparison.InvariantCultureIgnoreCase))
        {
            services.AddBallwareDocumentStorageForSqlServer(storageOptions, storageConnectionString);
        }
        else if ("postgres".Equals(storageOptions.Provider, StringComparison.InvariantCultureIgnoreCase))
        {
            services.AddBallwareDocumentStorageForPostgres(storageOptions, storageConnectionString);
        }

        return services;
    }

    public static IServiceCollection AddBallwareDocumentMetadataProviders(this IServiceCollection services)
    {
        var mapsterConfig = new TypeAdapterConfig()
            .AddBallwareDocumentStorageMappings();

        new MetaServiceDocumentMetadataProfile().Register(mapsterConfig);
        new GenericServiceDocumentMetadataProfile().Register(mapsterConfig);
        new MlServiceDocumentMetadataProfile().Register(mapsterConfig);

        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddHttpContextAccessor();

        services.AddScoped<IDocumentMetadataProvider, DocumentMetadataProvider>();
        services.AddScoped<IDocumentPickvalueProvider, MetaServiceProvider>();
        services.AddScoped<IDocumentLookupProvider, GenericServiceProvider>();
        services.AddScoped<IMetaDatasourceProvider, MetaServiceProvider>();
        services.AddScoped<IDocumentProcessingStateProvider, MetaServiceProvider>();
        services.AddScoped<IAuthorizationMetadataProvider, MetaServiceProvider>();

        services.AddScoped<IDatasourceDefinitionProvider, GenericServiceProvider>();
        services.AddScoped<IDatasourceDefinitionProvider, MetaServiceProvider>();
        services.AddScoped<IDatasourceDefinitionProvider, MlServiceProvider>();

        return services;
    }

    public static IServiceCollection AddBallwareDocumentAuthentication(this IServiceCollection services, BallwareDocumentOptions options, bool withSession, string dataProtectionApplicationName, string? dataProtectionKeysPath = null)
    {
        var authorizationOptions = options.Authorization;

        services.AddMemoryCache();

        if (withSession)
        {
            services.AddDistributedMemoryCache();

            var dataProtection = services.AddDataProtection();

            dataProtection.SetApplicationName(dataProtectionApplicationName);

            if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
            {
                dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
            }

            services.AddAuthentication(authenticationOptions =>
                {
                    authenticationOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    authenticationOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, openIdConnectOptions =>
                {
                    openIdConnectOptions.Authority = authorizationOptions.Authority;
                    openIdConnectOptions.ClientId = authorizationOptions.ClientId;
                    openIdConnectOptions.ClientSecret = null;
                    openIdConnectOptions.ResponseType = "code";
                    openIdConnectOptions.Scope.Add("openid");
                    openIdConnectOptions.Scope.Add("profile");
                    openIdConnectOptions.Scope.Add(authorizationOptions.RequiredUserScope);
                    openIdConnectOptions.RequireHttpsMetadata = authorizationOptions.RequireHttpsMetadata;
                    openIdConnectOptions.TokenValidationParameters.ValidIssuer = authorizationOptions.Issuer ?? authorizationOptions.Authority;

                    openIdConnectOptions.RegisterBallwareSessionTokenHandling();
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtBearerOptions =>
                {
                    jwtBearerOptions.MapInboundClaims = false;
                    jwtBearerOptions.Authority = authorizationOptions.Authority;
                    jwtBearerOptions.Audience = authorizationOptions.Audience;
                    jwtBearerOptions.RequireHttpsMetadata = authorizationOptions.RequireHttpsMetadata;
                    jwtBearerOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidIssuer = authorizationOptions.Issuer ?? authorizationOptions.Authority
                    };
                });

            services.AddSingleton<ConfigurationManager<OpenIdConnectConfiguration>>(sp =>
            {
                var openIdConnectOptions = sp.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                    .Get(OpenIdConnectDefaults.AuthenticationScheme);

                var retriever = new HttpDocumentRetriever { RequireHttps = authorizationOptions.RequireHttpsMetadata };

                return new ConfigurationManager<OpenIdConnectConfiguration>(
                    openIdConnectOptions.Authority!.TrimEnd('/') + "/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever(), retriever);
            });

            services.AddBallwareSession(options.Session);
        }
        else
        {
            services.AddAuthentication(authenticationOptions =>
                {
                    authenticationOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    authenticationOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtBearerOptions =>
                {
                    jwtBearerOptions.MapInboundClaims = false;
                    jwtBearerOptions.Authority = authorizationOptions.Authority;
                    jwtBearerOptions.Audience = authorizationOptions.Audience;
                    jwtBearerOptions.RequireHttpsMetadata = authorizationOptions.RequireHttpsMetadata;
                    jwtBearerOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidIssuer = authorizationOptions.Issuer ?? authorizationOptions.Authority
                    };
                });
        }

        services.AddAuthorizationBuilder()
            .AddPolicy("documentApi", policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User
                        .Claims
                        .Where(c => ClaimTypeScope == c.Type)
                        .SelectMany(c => c.Value.Split(' '))
                        .Any(s => s.Equals(authorizationOptions.RequiredUserScope, StringComparison.Ordinal)))
            )
            .AddPolicy("serviceApi", policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    context.User
                        .Claims
                        .Where(c => ClaimTypeScope == c.Type)
                        .SelectMany(c => c.Value.Split(' '))
                        .Any(s => s.Equals(ServiceApiScope, StringComparison.Ordinal)))
            );

        services.AddBallwareSharedAuthorizationUtils(authorizationOptions.TenantClaim, authorizationOptions.UserIdClaim, authorizationOptions.RightClaim);

        return services;
    }
}
