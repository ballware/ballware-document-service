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
            if (context.Request.Path.StartsWithSegments(pathPrefix, out var remainingPath)
                && !IsApiPath(remainingPath))
            {
                context.Request.PathBase = context.Request.PathBase.Add(pathPrefix);
                context.Request.Path = remainingPath;
            }

            return next();
        });
    }

    private static bool IsApiPath(PathString remainingPath) =>
        Array.Exists(ApiPathPrefixes, apiPathPrefix => remainingPath.StartsWithSegments(apiPathPrefix));
}
