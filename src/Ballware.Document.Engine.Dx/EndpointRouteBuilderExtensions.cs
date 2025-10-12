using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Ballware.Document.Engine.Dx.Components;

namespace Ballware.Document.Engine.Dx;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapBallwareDevExpressReporting(this IEndpointRouteBuilder builder)
    {
        builder.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        
        return builder;
    }
}