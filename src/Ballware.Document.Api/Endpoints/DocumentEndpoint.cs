using System.Security.Claims;
using Ballware.Document.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Quartz;

namespace Ballware.Document.Api.Endpoints;

public static class DocumentEndpoint
{
    public static IEndpointRouteBuilder MapDocumentUserApi(this IEndpointRouteBuilder app,
        string basePath,
        string apiTag = "Document",
        string apiOperationPrefix = "Document",
        string authorizationScope = "documentApi",
        string apiGroup = "document")
    {
        app.MapPost(basePath + "/updatedatasources", HandleUpdateDatasourcesByIdsAsync)
            .RequireAuthorization(authorizationScope)
            .DisableAntiforgery()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(apiOperationPrefix + "UpdateDatasourcesByIds")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Update document datasources by ids");
        
        return app;
    }

    private static async Task<IResult> HandleUpdateDatasourcesByIdsAsync(ISchedulerFactory schedulerFactory, IPrincipalUtils principalUtils, ClaimsPrincipal user, QueryValueBag query)
    {
        if (!query.Query.TryGetValue("id", out var ids))
        {
            return Results.BadRequest("No document ids provided.");
        }
        
        var tenantId = principalUtils.GetUserTenandId(user);
        var userId = principalUtils.GetUserId(user);
        
        foreach (var id in ids.Select(Guid.Parse))
        {
            var jobData = new JobDataMap
            {
                { "tenantId", tenantId },
                { "userId", userId },
                { "documentId", id }
            };
            
            await (await schedulerFactory.GetScheduler()).TriggerJob(JobKey.Create("updatedatasources", "document"), jobData);
        }

        return Results.Created();
    }
}