using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Ballware.Document.Session;

public interface ISessionPrincipalProvider
{
    Task<ClaimsPrincipal?> GetPrincipalAsync(string sessionId);
    Task StorePrincipalAsync(HttpContext context, ClaimsPrincipal principal);
}