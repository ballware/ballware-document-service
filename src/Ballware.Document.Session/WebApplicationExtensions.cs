using Ballware.Document.Session.Internal;
using Microsoft.AspNetCore.Builder;

namespace Ballware.Document.Session;

public static class WebApplicationExtensions
{
    public static WebApplication UseBallwareSession(this WebApplication app)
    {
        app.UseSession();
        app.UseMiddleware<CachedClaimsPrincipalMiddleware>();

        return app;
    }
}