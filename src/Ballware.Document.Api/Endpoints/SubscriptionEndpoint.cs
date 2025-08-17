using System.Security.Claims;
using Ballware.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Quartz;

namespace Ballware.Document.Api.Endpoints;

public static class SubscriptionEndpoint
{
    public static IEndpointRouteBuilder MapSubscriptionUserApi(this IEndpointRouteBuilder app,
        string basePath,
        string apiTag = "Subscription",
        string apiOperationPrefix = "Subscription",
        string authorizationScope = "documentApi",
        string apiGroup = "subscription")
    {
        app.MapPost(basePath + "/trigger", HandleTriggerByIdsAsync)
            .RequireAuthorization(authorizationScope)
            .DisableAntiforgery()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(apiOperationPrefix + "TriggerByIds")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Trigger subscriptions by ids");
        
        return app;
    }

    private static async Task<IResult> HandleTriggerByIdsAsync(ISchedulerFactory schedulerFactory, IPrincipalUtils principalUtils, ClaimsPrincipal user, QueryValueBag query)
    {
        if (!query.Query.TryGetValue("id", out var ids))
        {
            return Results.BadRequest("No subscription ids provided.");
        }
        
        var tenantId = principalUtils.GetUserTenandId(user);
        var userId = principalUtils.GetUserId(user);
        
        foreach (var id in ids.Select(Guid.Parse))
        {
            var jobData = new JobDataMap
            {
                { "tenantId", tenantId },
                { "userId", userId },
                { "subscriptionId", id }
            };
            
            await (await schedulerFactory.GetScheduler()).TriggerJob(JobKey.Create("trigger", "subscription"), jobData);
        }

        return Results.Created();
    }
}