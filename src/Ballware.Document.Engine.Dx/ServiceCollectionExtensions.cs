using Ballware.Document.Engine.Dx.Internal;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
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
        services.AddScoped<IDocumentDatasourceProvider, DocumentDatasourceProvider>();
        services.AddDevExpressControls();
        
        services.ConfigureReportingServices(builder =>
        {
            builder.UseDevelopmentMode(options =>
            {
                options.Enabled = true;
                options.EnableClientSideDevelopmentMode = true;
                options.CheckClientLibraryVersions = true;
            });
            
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