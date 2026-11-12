using Ballware.Document.Session;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Ballware.Document.App.Middleware;

// SignalR-Circuits und DX-Transport-Endpoints können einer OIDC-302-Challenge nicht folgen.
// Fehlende Authentifizierung liefert auf diesen Pfaden gezielt 401, damit das Frontend
// erneut über /signon initialisieren kann. Seitenrequests (/viewer, /designer) behalten
// die OIDC-Challenge (-> /signin-oidc -> Cookie).
public class ChallengeDifferentiationMiddleware
{
    private static readonly string[] TransportPathPrefixes =
    [
        "/_blazor",
        "/DXX",
        "/dxDR"
    ];

    private readonly RequestDelegate _next;

    public ChallengeDifferentiationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsTransportPath(context.Request.Path))
        {
            await _next(context);

            return;
        }

        var result = await context.AuthenticateAsync(SessionAuthenticationSchemes.DistributedSession);

        if (!result.Succeeded)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            return;
        }

        context.User = result.Principal;

        await _next(context);
    }

    private static bool IsTransportPath(PathString path) =>
        Array.Exists(TransportPathPrefixes, prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}
