using DevExpress.AspNetCore.Reporting.ReportDesigner.Native.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ballware.Document.Engine.Dx.Controllers;

[Route("api/ReportDesigner")]
public class ReportDesignerController : DevExpress.AspNetCore.Reporting.ReportDesigner.ReportDesignerController
{
    public ReportDesignerController(IReportDesignerMvcControllerService controllerService) : base(controllerService) { }
}