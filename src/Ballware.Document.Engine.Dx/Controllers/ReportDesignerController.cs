using DevExpress.AspNetCore.Reporting.ReportDesigner.Native.Services;
using DevExpress.XtraReports.Web.ReportDesigner.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ballware.Document.Engine.Dx.Controllers;

[Route("api/ReportDesigner")]
public class ReportDesignerController : DevExpress.AspNetCore.Reporting.ReportDesigner.ReportDesignerController
{
    private IReportDesignerModelBuilder ModelBuilder { get; }
    
    public ReportDesignerController(IReportDesignerMvcControllerService controllerService, IReportDesignerModelBuilder reportDesignerModelBuilder) : base(controllerService)
    {
        ModelBuilder = reportDesignerModelBuilder;
    }
    
    [Route("GetReportDesignerModel")]
    public async Task<IActionResult> GetReportDesignerModel(string reportUrl)
    {
        //var tenantId = Guid.Parse(User.FindFirstValue("tenant"));
        //var rights = User.Claims?.Where(cl => cl.Type == "right").Select(cl => cl.Value) ?? new List<string>();

        var designerModel = await ModelBuilder
            .Report(reportUrl)
            //.DataSources(await DatasourceProvider.GetAvailableDataSourcesAsync(tenantId))
            //.DataSourceSettings(config =>
            //{
            //    config.AllowAddDataSource = rights.Contains("meta.document.edit");
            //    config.AllowEditDataSource = rights.Contains("meta.document.edit");
            //    config.AllowRemoveDataSource = rights.Contains("meta.document.edit");
            //})
            .DesignerUri("api/ReportDesigner")
            .ViewerUri("api/DocumentViewer")
            .QueryBuilderUri("api/QueryBuilder")
            .BuildModelAsync();

        return DesignerModel(designerModel);
    }
}