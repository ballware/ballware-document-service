using DevExpress.AspNetCore.Reporting.QueryBuilder.Native.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ballware.Document.Engine.Dx.Controllers;

[Route("api/QueryBuilder")]
public class QueryBuilderController : DevExpress.AspNetCore.Reporting.QueryBuilder.QueryBuilderController
{
    public QueryBuilderController(IQueryBuilderMvcControllerService controllerService) : base(controllerService) { }
}