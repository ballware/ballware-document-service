using Ballware.Document.Engine.Dx.Internal;
using Ballware.Document.Metadata;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using DevExpress.Blazor.Reporting;
using DevExpress.XtraReports.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareDevExpressReporting(this IServiceCollection services)
    {   
        services.AddDevExpressBlazor();
        services.AddDevExpressBlazorReporting();
        services.AddSingleton<LoggerService>();
        services.AddSingleton<ReportStorageWebExtension, DocumentStorage>();
        services.AddSingleton<FetchNameByIdFromLookup>();
        services.AddSingleton<FetchNameByStateFromProcessingState>();
        services.AddSingleton<FetchTextByValueFromPickvalue>();
        services.AddScoped<IDocumentDatasourceProvider, DocumentDatasourceProvider>();
        services.AddScoped<IDocumentModificationProvider, DocumentModificationProvider>();
        services.AddScoped<IDocumentMailGenerator, DocumentMailGenerator>();
        
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

        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        return services;
    }
}