using Ballware.Document.Session.Configuration;
using Ballware.Document.Session.Internal;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Session;

public static class OpenIdConnectOptionsExtensions
{
    public static void RegisterBallwareSessionTokenHandling(this OpenIdConnectOptions options)
    {
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var httpContext = context.HttpContext;
                var sessionOptions = httpContext.RequestServices.GetRequiredService<SessionOptions>();
                var sessionProvider = httpContext.RequestServices.GetRequiredService<SessionPrincipalProvider>();

                var principal = context.Principal!;
                await sessionProvider.StorePrincipalAsync(httpContext, principal, context.Properties?.RedirectUri);

                if (sessionOptions.SuppressDefaultSessionCookie)
                {
                    var returnUrl = context.Properties?.RedirectUri;

                    if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
                    {
                        returnUrl = "/";
                    }

                    context.HandleResponse();
                    httpContext.Response.Redirect(returnUrl!);
                }
            }
        };
    }
}