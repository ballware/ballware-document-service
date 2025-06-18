using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

namespace Ballware.Document.Service.Configuration;

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

    public bool RequireHttpsMetadata { get; set; } = true;
    
    [Required]
    public required string RequiredUserScope { get; set; } = "documentApi";
}