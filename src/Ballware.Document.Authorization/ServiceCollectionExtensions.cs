using Ballware.Document.Authorization.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Authorization;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareDocumentAuthorizationUtils(this IServiceCollection services, string tenantClaim, string userIdClaim, string rightClaim)
    {
        services.AddSingleton<IPrincipalUtils>(new DefaultPrincipalUtils(tenantClaim, userIdClaim, rightClaim));

        return services;
    }
}