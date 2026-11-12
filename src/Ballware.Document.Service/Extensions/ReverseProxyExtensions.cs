using Ballware.Document.Service.Configuration;

namespace Ballware.Document.Service.Extensions;

public static class ReverseProxyExtensions
{
    // Die Api-Endpunkte sind inklusive des Gateway-Prefix gemappt, z. B. "/document/document".
    // Ihre Pfade bleiben unverändert, alles andere unter dem Prefix sind Client-Ressourcen,
    // die die Anwendung ohne Prefix mappt und die dann über das PathBase beantwortet werden.
    private static readonly string[] ApiPathPrefixes =
    [
        "/document",
        "/notification",
        "/subscription",
        "/processingstate"
    ];

    public static IApplicationBuilder UseReverseProxyPathBase(this IApplicationBuilder app, ReverseProxyOptions options)
    {
        var pathPrefix = new PathString(options.PathPrefix);

        if (pathPrefix == PathString.Empty)
        {
            return app;
        }

        return app.Use((context, next) =>
        {
            var requestPath = context.Request.Path;

            if (context.Request.PathBase != PathString.Empty || IsApiPath(requestPath))
            {
                return next();
            }

            if (requestPath.StartsWithSegments(pathPrefix, out var remainingPath))
            {
                // Der Gateway leitet den Prefix unverändert weiter: er wird aus dem Pfad abgebaut.
                if (IsApiPath(remainingPath))
                {
                    return next();
                }

                context.Request.PathBase = pathPrefix;
                context.Request.Path = remainingPath;

                return next();
            }

            // Der Gateway hat den Prefix bereits abgebaut, sodass er der Anwendung nicht mehr
            // bekannt ist. Ohne PathBase erzeugten die Komponenten absolute Ressourcen-URLs
            // ohne Prefix (NavigationManager.BaseUri), z. B. die DevExpress-Report-Stylesheets,
            // und das Gateway beantwortete sie nicht.
            context.Request.PathBase = pathPrefix;

            return next();
        });
    }

    private static bool IsApiPath(PathString path) =>
        Array.Exists(ApiPathPrefixes, apiPathPrefix => path.StartsWithSegments(apiPathPrefix));
}
