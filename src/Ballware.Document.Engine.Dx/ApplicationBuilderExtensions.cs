using Ballware.Document.Engine.Dx.Internal;
using DevExpress.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseBallwareDevExpressReporting(this IApplicationBuilder builder)
    {
        var documentStorage = builder.ApplicationServices.GetRequiredService<DocumentStorage>();
        var loggerService = builder.ApplicationServices.GetRequiredService<LoggerService>();
        
        DevExpress.XtraReports.Web.Extensions.ReportStorageWebExtension.RegisterExtensionGlobal(documentStorage);
        DevExpress.XtraReports.Web.ClientControls.LoggerService.Initialize(loggerService);
        
        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchNameByIdFromLookup>());
        
        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchTextByValueFromPickvalue>());
        
        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchNameByStateFromProcessingState>());
        
        builder.UseDevExpressControls();
        
        return builder;
    }
}