using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ballware.Document.Hosting.Configuration;
using Ballware.Document.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Ballware.Document.Hosting;

public static class SignOnEndpointExtensions
{
    private static readonly PathString ViewerPath = new("/viewer");
    private static readonly PathString DesignerPath = new("/designer");

    public static IEndpointRouteBuilder MapBallwareDocumentSignOnEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/signon/{idToken}", HandleSignOnAsync)
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleSignOnAsync(HttpContext context, ISessionPrincipalProvider principalProvider, IOptions<AuthorizationOptions> options, ConfigurationManager<OpenIdConnectConfiguration> configurationManager, string idToken, string? redirect)
    {
        if (!IsSafeRedirect(redirect, context.Request, options.Value.SignOnAllowedHosts))
        {
            return Results.BadRequest("Only redirects to /, /viewer or /designer on the request host or a configured sign-on host are allowed.");
        }

        var config = await configurationManager.GetConfigurationAsync();

        var handler = new JsonWebTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = options.Value.Issuer ?? options.Value.Authority,
            ValidAudience = options.Value.ClientId,
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true,
        };

        if (!handler.CanReadToken(idToken))
        {
            return Results.Unauthorized();
        }

        var jsonWebToken = handler.ReadJsonWebToken(idToken);

        var result = await handler.ValidateTokenAsync(jsonWebToken, validationParameters);

        if (result.IsValid)
        {
            var identity = new ClaimsIdentity(jsonWebToken.Claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            await principalProvider.StorePrincipalAsync(context, principal);

            return Results.Redirect(redirect);
        }

        return Results.Unauthorized();
    }

    private static bool IsSafeRedirect([NotNullWhen(true)] string? redirect, HttpRequest request, string[] allowedHosts)
    {
        if (string.IsNullOrWhiteSpace(redirect))
        {
            return false;
        }

        // Kompatibilität zum Frontend: absolute http(s)-Redirect-URLs sind erlaubt, aber nur auf denselben
        // Host wie die Anfrage selbst oder auf einem per Authorization:SignOnAllowedHosts konfigurierten
        // Host (Reverse-Proxies liefern meist nicht den öffentlichen Host, z. B. nur "reporting-app").
        // Open-Redirect auf nicht konfigurierte fremde Hosts bleibt geschlossen.
        // Achtung: Uri.TryCreate interpretiert "/viewer" als absoluten file-URI — diese Fälle fallen
        // bewusst in den relativen Zweig zurück.
        if (Uri.TryCreate(redirect, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            var requestHostMatch = string.Equals(absolute.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase);

            var allowedHostMatch = allowedHosts.Any(host => !string.IsNullOrWhiteSpace(host)
                && string.Equals(absolute.Host, TrimToHost(host), StringComparison.OrdinalIgnoreCase));

            if (!requestHostMatch && !allowedHostMatch)
            {
                return false;
            }

            // Für den konfigurierten öffentlichen Host gilt die Port-Regel nicht (der Proxy liefert keinen Port).
            if (requestHostMatch && !allowedHostMatch)
            {
                var isDefaultPort = absolute.Scheme == Uri.UriSchemeHttp ? absolute.Port == 80 : absolute.Port == 443;

                if (!isDefaultPort && request.Host.Port != absolute.Port)
                {
                    return false;
                }
            }

            return IsSafePath(absolute.AbsolutePath);
        }

        if (!Uri.TryCreate(redirect, UriKind.Relative, out _))
        {
            return false;
        }

        return IsSafePath(redirect);
    }

    private static string TrimToHost(string value)
    {
        var host = value.Trim();

        var schemeIndex = host.IndexOf("://", StringComparison.Ordinal);

        if (schemeIndex >= 0)
        {
            host = host.Substring(schemeIndex + 3);
        }

        var end = host.IndexOfAny(['/', ':', '?', '#']);

        if (end >= 0)
        {
            host = host.Substring(0, end);
        }

        return host;
    }

    private static bool IsSafePath(string rawPath)
    {
        // Percent-Dekodierung vor der Prüfung, damit kodierte Umwege (%2F, %2E) nicht durchrutschen.
        var decoded = Uri.UnescapeDataString(rawPath);

        var queryIndex = decoded.IndexOf('?');
        var path = queryIndex >= 0 ? decoded.Substring(0, queryIndex) : decoded;
        if (!path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        if (path.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (path == "/")
        {
            return true;
        }

        var pathString = new PathString(path);

        return pathString.StartsWithSegments(ViewerPath) || pathString.StartsWithSegments(DesignerPath);
    }
}
