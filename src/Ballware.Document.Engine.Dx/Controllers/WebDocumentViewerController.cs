using DevExpress.AspNetCore.Reporting.WebDocumentViewer.Native.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ballware.Document.Engine.Dx.Controllers;

[Route("api/DocumentViewer")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class WebDocumentViewerController : DevExpress.AspNetCore.Reporting.WebDocumentViewer.WebDocumentViewerController
{
    public WebDocumentViewerController(IWebDocumentViewerMvcControllerService controllerService) : base(
        controllerService)
    {
        
    }
}