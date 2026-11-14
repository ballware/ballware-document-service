using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

namespace Ballware.Document.Hosting.Configuration;

public class AuthorizationOptions
{
    [Required]
    public required string Authority { get; set; }

    [Required]
    public required string Audience { get; set; }
    
    [Required]
    public required string ClientId { get; set; }
    
    [Required]
    public required string TenantClaim { get; set; } = "tenant";

    [Required]
    public required string UserIdClaim { get; set; } = JwtRegisteredClaimNames.Sub;

    [Required]
    public required string RightClaim { get; set; } = "right";

    public string? Issuer { get; set; }
    
    public bool RequireHttpsMetadata { get; set; } = true;

    public string[] SignOnAllowedHosts { get; set; } = [];

    // Öffentliche Basis-URL, unter der die App vom Browser erreicht wird, z. B.
    // https://reporting.ballware.de bzw. http://localhost:3003. Der redirect_uri der
    // OIDC-Challenge wird deterministisch daraus abgeleitet (Basis-URL + CallbackPath) und muss
    // in genau dieser Form als gültige Redirect-URI für ClientId beim IdP registriert sein.
    // Wird nichts konfiguriert, bleibt es bei der Herleitung aus Scheme/Host des eingehenden
    // Requests (Anzeige der verwendeten URL im Log bzw. bei Challenge-Fehlern).
    public string? PublicBaseUrl { get; set; }

    [Required]
    public required string RequiredUserScope { get; set; } = "documentApi";
}