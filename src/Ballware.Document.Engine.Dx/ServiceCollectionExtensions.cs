using Ballware.Document.Engine.Dx.Internal;
using Ballware.Document.Metadata;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using DevExpress.Blazor.Reporting;
using DevExpress.XtraReports.Web.Extensions;
using DevExpress.XtraReports.Web.WebDocumentViewer.Native.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareDevExpressReportingCore(this IServiceCollection services)
    {
        services.AddSingleton<LoggerService>();
        services.AddSingleton<FetchNameByIdFromLookup>();
        services.AddSingleton<FetchNameByStateFromProcessingState>();
        services.AddSingleton<FetchTextByValueFromPickvalue>();
        services.AddScoped<IDocumentDatasourceProvider, DocumentDatasourceProvider>();
        services.AddScoped<IDocumentModificationProvider, DocumentModificationProvider>();
        services.AddScoped<IDocumentMailGenerator, DocumentMailGenerator>();

        services.ConfigureReportingServices(builder =>
        {
            builder.ConfigureReportDesigner(designerConfigurator =>
            {
                designerConfigurator.EnableCustomSql();
            });
        });

        return services;
    }

    public static IServiceCollection AddBallwareDevExpressReportingBlazor(this IServiceCollection services)
    {
        services.AddBallwareDevExpressReportingCore();

        services.AddDevExpressBlazorReporting();

        // Standardmäßig registriert die ASP.NET-Core-Reporting-Konfiguration
        // AsyncModeSettings(useReportResolverAsync: false). Die synchrone Report-Auflösung ruft
        // ReportStorageWebExtension.GetData direkt auf dem Blazor DispatcherSyncContext, wo
        // DocumentStorage die async Meta-Zugriffe blockieren muss -> Deadlock im Circuit.
        // Async-Mode erzwingt die ResolveAsync/GetDataAsync-Kette (analog zu DX AddDevExpressBlazorReportingWebAssembly).
        services.Add(new ServiceDescriptor(typeof(AsyncModeSettings), (object)new AsyncModeSettings(useReportResolverAsync: true)));

        services.AddScoped<ReportStorageWebExtension, DocumentStorage>();

        return services;
    }
}
