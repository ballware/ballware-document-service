using Ballware.Document.Session.Configuration;
using Ballware.Document.Session.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Session;

public static class SessionAuthenticationSchemes
{
    public const string DistributedSession = "DistributedSession";
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareSession(this IServiceCollection services, SessionOptions options)
    {
        services.AddSession();
        services.AddSingleton(options);
        services.AddSingleton<ISessionPrincipalProvider, SessionPrincipalProvider>();
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, SessionPrincipalAuthenticationHandler>(SessionAuthenticationSchemes.DistributedSession, null);
        
        return services;
    }
}