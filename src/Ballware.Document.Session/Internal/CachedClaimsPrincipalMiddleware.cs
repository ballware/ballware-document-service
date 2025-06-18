using Microsoft.AspNetCore.Http;
using Ballware.Document.Session.Configuration;

namespace Ballware.Document.Session.Internal;

class CachedClaimsPrincipalMiddleware
{
    private readonly RequestDelegate _next;
    
    private SessionPrincipalProvider SessionPrincipalProvider { get; }
    private SessionOptions Options { get; }

    public CachedClaimsPrincipalMiddleware(RequestDelegate next, SessionPrincipalProvider provider, SessionOptions options)
    {
        _next = next;
        SessionPrincipalProvider = provider;
        Options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true && context.Request.Cookies.TryGetValue(Options.SessionCookieName, out var sessionId))
        {
            var principal = await SessionPrincipalProvider.GetPrincipalAsync(sessionId);

            if (principal != null)
            {
                context.User = principal;
            }
        }

        await _next(context);
    }
}