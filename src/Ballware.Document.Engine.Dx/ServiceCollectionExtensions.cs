using Ballware.Document.Engine.Dx.Internal;
using Ballware.Document.Engine.Dx.Pages;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareDevExpressReporting(this IServiceCollection services)
    {
        services.AddSingleton<LoggerService>();
        services.AddSingleton<DocumentStorage>();
        services.AddSingleton<FetchNameByIdFromLookup>();
        services.AddSingleton<FetchNameByStateFromProcessingState>();
        services.AddSingleton<FetchTextByValueFromPickvalue>();
        services.AddDevExpressControls();
        
        services.ConfigureReportingServices(builder =>
        {
            builder.UseDevelopmentMode();
            builder.ConfigureReportDesigner(designerConfigurator =>
            {
                designerConfigurator.EnableCustomSql();
            });
        });

        services.AddMvc();
        
        services.AddControllers()
            .AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly);

        services.AddRazorPages()
            .AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly);
            
        return services;
    }
}