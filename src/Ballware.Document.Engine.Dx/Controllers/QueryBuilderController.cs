using DevExpress.AspNetCore.Reporting.QueryBuilder.Native.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ballware.Document.Engine.Dx.Controllers;

[Route("api/QueryBuilder")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class QueryBuilderController : DevExpress.AspNetCore.Reporting.QueryBuilder.QueryBuilderController
{
    public QueryBuilderController(IQueryBuilderMvcControllerService controllerService) : base(controllerService) { }
}