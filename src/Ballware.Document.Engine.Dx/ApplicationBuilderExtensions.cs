using Ballware.Document.Engine.Dx.Internal;
using DevExpress.AspNetCore;
using DevExpress.Blazor.Reporting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseBallwareDevExpressReportingCore(this IApplicationBuilder builder)
    {
        var loggerService = builder.ApplicationServices.GetRequiredService<LoggerService>();

        DevExpress.XtraReports.Web.ClientControls.LoggerService.Initialize(loggerService);

        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchNameByIdFromLookup>());

        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchTextByValueFromPickvalue>());

        DevExpress.Data.Filtering.CriteriaOperator.RegisterCustomFunction(
            builder.ApplicationServices.GetRequiredService<FetchNameByStateFromProcessingState>());

        return builder;
    }

    public static IApplicationBuilder UseBallwareDevExpressReportingBlazor(this IApplicationBuilder builder)
    {
        builder.UseBallwareDevExpressReportingCore();

        builder.UseDevExpressBlazorReporting();

        return builder;
    }
}
