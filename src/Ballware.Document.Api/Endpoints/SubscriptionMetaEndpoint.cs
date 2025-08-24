using System.Security.Claims;
using Ballware.Shared.Api.Endpoints.Bindings;
using Ballware.Shared.Authorization;
using Ballware.Document.Data.Public;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Quartz;

namespace Ballware.Document.Api.Endpoints;

public static class SubscriptionMetaEndpoint
{
    private const string ApiTag = "Subscription";
    private const string ApiOperationPrefix = "Subscription";
    
    public static IEndpointRouteBuilder MapSubscriptionUserApi(this IEndpointRouteBuilder app, 
        string basePath,
        string apiTag = ApiTag,
        string apiOperationPrefix = ApiOperationPrefix,
        string authorizationScope = "documentApi",
        string apiGroup = "subscription")
    {
        app.MapGet(basePath + "/selectlist", HandleSelectListAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<IEnumerable<SubscriptionSelectListEntry>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "SelectList")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query list of all pages");
        
        app.MapGet(basePath + "/selectbyid/{id}", HandleSelectByIdAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<SubscriptionSelectListEntry>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(apiOperationPrefix + "SelectById")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query select item by id");
        
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

    public static IEndpointRouteBuilder MapSubscriptionServiceApi(this IEndpointRouteBuilder app,
        string basePath,
        string apiTag = ApiTag,
        string apiOperationPrefix = ApiOperationPrefix,
        string authorizationScope = "serviceApi",
        string apiGroup = "service")
    {
        app.MapGet(basePath + "/metadatabytenantandid/{tenantId}/{id}", HandleMetadataForTenantAndIdAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<Subscription>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(apiOperationPrefix + "MetadataByTenantAndId")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query subscription metadata by tenant and id");

        app.MapGet(basePath + "/activebytenantandfrequency/{tenantId}/{frequency}", HandleActiveSubscriptionsForTenantAndFrequencyAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<IEnumerable<Subscription>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "ActiveByFrequency")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query active subscriptions by frequency");
        
        app.MapPost(basePath + "/setsendresult/{tenantId}/{id}", HandleSetSendResult)
            .RequireAuthorization(authorizationScope)
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "SetSendResult")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Send send result for subscription");
        
        return app;
    }
    
    private static async Task<IResult> HandleSelectListAsync(IPrincipalUtils principalUtils, ISubscriptionMetaRepository repository, ClaimsPrincipal user)
    {
        var tenantId = principalUtils.GetUserTenandId(user);

        return Results.Ok(await repository.SelectListForTenantAsync(tenantId));
    }
    
    private static async Task<IResult> HandleSelectByIdAsync(IPrincipalUtils principalUtils, ISubscriptionMetaRepository repository, ClaimsPrincipal user, Guid id)
    {
        var tenantId = principalUtils.GetUserTenandId(user);
        
        var entry = await repository.SelectByIdForTenantAsync(tenantId, id);
        
        if (entry == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(entry);
    }
    
    private static async Task<IResult> HandleMetadataForTenantAndIdAsync(ISubscriptionMetaRepository repository, Guid tenantId, Guid id)
    {
        var subscription = await repository.MetadataByTenantAndIdAsync(tenantId, id);
        
        if (subscription == null)
        {
            return Results.NotFound();
        }
        
        return Results.Ok(subscription);
    }

    private static async Task<IResult> HandleActiveSubscriptionsForTenantAndFrequencyAsync(ISubscriptionMetaRepository repository,
        Guid tenantId, int frequency)
    {
        var subscriptions = await repository.GetActiveSubscriptionsByTenantAndFrequencyAsync(tenantId, frequency);
        
        return Results.Ok(subscriptions);
    }

    private static async Task<IResult> HandleSetSendResult(ISubscriptionMetaRepository repository, Guid tenantId,
        Guid id, [FromBody] string error)
    {
        await repository.SetLastErrorAsync(tenantId, id, error);

        return Results.Ok();
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