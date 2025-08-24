using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ballware.Document.Service.Configuration;
using Ballware.Document.Session;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Ballware.Document.Service.Endpoints;

public static class SignOnEndpoint
{
    public static IEndpointRouteBuilder MapSignOnEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/signon/{idToken}", HandleSignOnAsync)
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleSignOnAsync(HttpContext context, ISessionPrincipalProvider principalProvider, IOptions<AuthorizationOptions> options, ConfigurationManager<OpenIdConnectConfiguration> configurationManager, string idToken, string redirect)
    {
        var config = await configurationManager.GetConfigurationAsync();
        
        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = options.Value.Authority,
            ValidAudience = options.Value.ClientId,
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true,
        };

        var token = handler.ReadJwtToken(idToken);
        
        var result = await handler.ValidateTokenAsync(idToken, validationParameters);

        if (result.IsValid)
        {
            var identity = new ClaimsIdentity(token.Claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
        
            await principalProvider.StorePrincipalAsync(context, principal);
        
            return Results.Redirect(redirect);    
        }
        
        return Results.Unauthorized();
    }

}