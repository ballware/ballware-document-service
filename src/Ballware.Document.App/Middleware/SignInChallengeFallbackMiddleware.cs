using Ballware.Document.Hosting;
using Ballware.Document.Hosting.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;

namespace Ballware.Document.App.Middleware;

// Scheitert die OIDC-Challenge selbst — typischerweise weil die verwendete redirect_uri nicht als
// gültige Redirect-URI für den Client beim IdP registriert ist und der IdP den
// Pushed-Authorization-Request mit 400 ablehnt — lief die Exception unverarbeitet durch die
// Autorisierungsmiddleware und endete als 500. Ausgeliefert wird gezielt 401, damit Requests
// kontrolliert über /signon re-initialisiert werden können; die verwendete redirect_uri geht ins Log.
public class SignInChallengeFallbackMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthorizationOptions _authorizationOptions;

    public SignInChallengeFallbackMiddleware(RequestDelegate next, IOptions<AuthorizationOptions> authorizationOptions)
    {
        _next = next;
        _authorizationOptions = authorizationOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OpenIdConnectProtocolException exception) when (!context.Response.HasStarted)
        {
            var redirectUri = context.Items[OpenIdConnectPublicRedirectExtensions.EffectiveRedirectUriItemKey] as string;

            Log.Error(
                exception,
                "OIDC sign-in challenge failed for {Path}. redirect_uri {RedirectUri} was used for client {ClientId}, Authorization:PublicBaseUrl is {PublicBaseUrl}. Register the callback path as valid redirect URI for this client at the identity provider.",
                context.Request.Path.HasValue ? context.Request.Path.Value : string.Empty,
                redirectUri ?? "<unknown>",
                _authorizationOptions.ClientId,
                string.IsNullOrWhiteSpace(_authorizationOptions.PublicBaseUrl) ? "<not configured>" : _authorizationOptions.PublicBaseUrl);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }
}
