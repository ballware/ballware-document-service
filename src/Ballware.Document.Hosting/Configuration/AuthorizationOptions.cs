using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

namespace Ballware.Document.Hosting.Configuration;

// Der Abschnitt "Authorization" wird geteilt: die Browser-Authentifizierung des App-Hosts
// (Authority, Audience, ClientId, RequiredUserScope, PublicBaseUrl, SignOnAllowedHosts, …) bindet
// Ballware.Shared.Web.Oidc als BrowserAuthorizationOptions; hier stehen nur die für die document
// Service-API und die Mandant-Auswertung benötigten Werte.
public class AuthorizationOptions
{
    [Required]
    public required string Authority { get; set; }

    [Required]
    public required string Audience { get; set; }

    [Required]
    public required string TenantClaim { get; set; } = "tenant";

    [Required]
    public required string UserIdClaim { get; set; } = JwtRegisteredClaimNames.Sub;

    [Required]
    public required string RightClaim { get; set; } = "right";

    public string? Issuer { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;

    [Required]
    public required string RequiredUserScope { get; set; } = "documentApi";
}
