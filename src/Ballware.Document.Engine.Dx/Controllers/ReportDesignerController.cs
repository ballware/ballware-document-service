using Ballware.Document.Authorization;
using DevExpress.AspNetCore.Reporting.ReportDesigner.Native.Services;
using DevExpress.XtraReports.Web.ReportDesigner.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ballware.Document.Engine.Dx.Controllers;

[Route("api/ReportDesigner")]
[Authorize(AuthenticationSchemes = "DistributedSession")]
public class ReportDesignerController : DevExpress.AspNetCore.Reporting.ReportDesigner.ReportDesignerController
{
    private IPrincipalUtils PrincipalUtils { get; }
    private IReportDesignerModelBuilder ModelBuilder { get; }
    private IDocumentDatasourceProvider DocumentDatasourceProvider { get; }
    
    public ReportDesignerController(IReportDesignerMvcControllerService controllerService, IPrincipalUtils principalUtils, IReportDesignerModelBuilder reportDesignerModelBuilder, IDocumentDatasourceProvider datasourceProvider) : base(controllerService)
    {
        PrincipalUtils = principalUtils;
        ModelBuilder = reportDesignerModelBuilder;
        DocumentDatasourceProvider = datasourceProvider;
    }
    
    [Route("GetReportDesignerModel")]
    public async Task<IActionResult> GetReportDesignerModel(string reportUrl)
    {
        var tenantId = PrincipalUtils.GetUserTenandId(User);
        var rights = PrincipalUtils.GetUserRights(User).ToList();
        
        var designerModel = await ModelBuilder
            .Report(reportUrl)
            .DataSources(DocumentDatasourceProvider.CreateDatasourcesForTenant(tenantId))
            .DataSourceSettings(config =>
            {
                config.AllowAddDataSource = rights.Contains("meta.document.edit");
                config.AllowEditDataSource = rights.Contains("meta.document.edit");
                config.AllowRemoveDataSource = rights.Contains("meta.document.edit");
            })
            .DesignerUri("api/ReportDesigner")
            .ViewerUri("api/DocumentViewer")
            .QueryBuilderUri("api/QueryBuilder")
            .BuildModelAsync();

        return DesignerModel(designerModel);
    }
}