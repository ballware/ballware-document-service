using Ballware.Document.Engine.Dx.Internal;
using DevExpress.AspNetCore;
using DevExpress.Blazor.Reporting;
using DevExpress.XtraReports.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseBallwareDevExpressReporting(this IApplicationBuilder builder)
    {
        /*
        var documentStorage = builder.ApplicationServices.GetRequiredService<ReportStorageWebExtension>();
        var loggerService = builder.ApplicationServices.GetRequiredService<LoggerService>();
        
        DevExpress.XtraReports.Web.Extensions.ReportStorageWebExtension.RegisterExtensionGlobal(documentStorage);
        DevExpress.XtraReports.Web.ClientControls.LoggerService.Initialize(loggerService);
        
        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchNameByIdFromLookup>());
        
        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchTextByValueFromPickvalue>());
        
        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchNameByStateFromProcessingState>());
        */
        builder.UseDevExpressBlazorReporting();
        
        return builder;
    }
}