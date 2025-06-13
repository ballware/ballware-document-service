using DevExpress.AspNetCore.Reporting.WebDocumentViewer.Native.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ballware.Document.Engine.Dx.Controllers;

[Route("api/WebDocumentViewer")]
public class WebDocumentViewerController : DevExpress.AspNetCore.Reporting.WebDocumentViewer.WebDocumentViewerController
{
    public WebDocumentViewerController(IWebDocumentViewerMvcControllerService controllerService) : base(
        controllerService)
    {
        
    }
}