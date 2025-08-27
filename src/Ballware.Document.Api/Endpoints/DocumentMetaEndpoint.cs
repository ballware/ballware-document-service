using System.Collections.Immutable;
using System.Security.Claims;
using Ballware.Shared.Api.Endpoints.Bindings;
using Ballware.Shared.Authorization;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Quartz;

namespace Ballware.Document.Api.Endpoints;

public static class DocumentMetaEndpoint
{
    private const string ApiTag = "Document";
    private const string ApiOperationPrefix = "Document";
    
    public static IEndpointRouteBuilder MapDocumentUserApi(this IEndpointRouteBuilder app, 
        string basePath,
        string apiTag = ApiTag,
        string apiOperationPrefix = ApiOperationPrefix,
        string authorizationScope = "documentApi",
        string apiGroup = "document")
    {
        app.MapGet(basePath + "/selectlistdocumentsforentity/{entity}", HandleSelectListForEntityAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<IEnumerable<DocumentSelectListEntry>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "SelectListForEntity")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query available documents for entity");
        
        app.MapGet(basePath + "/selectlist", HandleSelectListAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<IEnumerable<DocumentSelectListEntry>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "SelectList")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query list of all documents");
        
        app.MapGet(basePath + "/selectbyid/{id}", HandleSelectByIdAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<DocumentSelectListEntry>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(apiOperationPrefix + "SelectById")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query select item by id");
        
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

    public static IEndpointRouteBuilder MapDocumentServiceApi(this IEndpointRouteBuilder app,
        string basePath,
        string apiTag = ApiTag,
        string apiOperationPrefix = ApiOperationPrefix,
        string authorizationScope = "serviceApi",
        string apiGroup = "service")
    {   
        app.MapGet(basePath + "/selectlistdocumentsfortenant/{tenantId}", HandleSelectListForTenantAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<IEnumerable<DocumentSelectListEntry>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "SelectListForTenant")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query available documents for tenant");
        
        app.MapGet(basePath + "/documentmetadatabytenantandid/{tenantId}/{id}", HandleMetadataForTenantAndIdAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<Data.Public.Document>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(apiOperationPrefix + "MetadataForTenantAndId")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query document metadata by tenant and id");
        
        app.MapGet(basePath + "/documenttemplatebehalfofuserbytenant/{tenantId}", HandleNewForTenantAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<Data.Public.Document>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "NewForTenantAndUser")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Query new document template for tenant");
        
        app.MapPost(basePath + "/savedocumentbehalfofuser/{tenantId}/{userId}", HandleSaveForTenantBehalfOfUserAsync)
            .RequireAuthorization(authorizationScope)
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "SaveForTenantBehalfOfUser")
            .WithGroupName(apiGroup)
            .WithTags(apiTag)
            .WithSummary("Save document behalf of user");
        
        return app;
    }
    
    internal static async Task<IResult> HandleSelectListForEntityAsync(IPrincipalUtils principalUtils, ITenantRightsChecker tenantRightsChecker, IAuthorizationMetadataProvider authorizationMetaProvider, IDocumentMetaRepository repository, ClaimsPrincipal user, string entity)
    {
        var tenantId = principalUtils.GetUserTenandId(user);
        var claims = principalUtils.GetUserClaims(user);

        var tenant = await authorizationMetaProvider.MetadataForTenantByIdAsync(tenantId);
        
        if (tenant == null)
        {
            return Results.NotFound("Tenant not found");
        }
        
        var documentList = (await repository.SelectListForTenantAndEntityAsync(tenantId, entity))
            .ToAsyncEnumerable()
            .WhereAwait(async d =>
                await tenantRightsChecker.HasRightAsync(tenant, "meta", "document", claims,
                    $"visiblestate.{d.State}"))
            .ToEnumerable();

        return Results.Ok(documentList);
    }
    
    internal static async Task<IResult> HandleSelectListAsync(IPrincipalUtils principalUtils, IDocumentMetaRepository repository, ClaimsPrincipal user)
    {
        var tenantId = principalUtils.GetUserTenandId(user);

        return Results.Ok(await repository.SelectListForTenantAsync(tenantId));
    }
    
    internal static async Task<IResult> HandleSelectByIdAsync(IPrincipalUtils principalUtils, IDocumentMetaRepository repository, ClaimsPrincipal user, Guid id)
    {
        var tenantId = principalUtils.GetUserTenandId(user);

        var entry = await repository.SelectByIdForTenantAsync(tenantId, id);

        if (entry == null)
        {
            return Results.NotFound();
        }
        
        return Results.Ok(entry);
    }
    
    internal static async Task<IResult> HandleSelectListForTenantAsync(IDocumentMetaRepository repository, Guid tenantId)
    {
        return Results.Ok(await repository.SelectListForTenantAsync(tenantId));
    }
    
    internal static async Task<IResult> HandleMetadataForTenantAndIdAsync(IDocumentMetaRepository repository, Guid tenantId, Guid id)
    {
        var entry = await repository.MetadataByTenantAndIdAsync(tenantId, id);

        if (entry == null)
        {
            return Results.NotFound();
        }
        
        return Results.Ok(entry);
    }
    
    internal static async Task<IResult> HandleNewForTenantAsync(IDocumentMetaRepository repository, Guid tenantId)
    {
        return Results.Ok(await repository.NewAsync(tenantId, "primary", ImmutableDictionary<string, object>.Empty));
    }
    
    internal static async Task<IResult> HandleSaveForTenantBehalfOfUserAsync(IDocumentMetaRepository repository, Guid tenantId, Guid userId, [FromBody] Data.Public.Document payload)
    {
        await repository.SaveAsync(tenantId, userId, "primary", ImmutableDictionary<string, object>.Empty, payload);
            
        return Results.Ok();
    }
    
    internal static async Task<IResult> HandleUpdateDatasourcesByIdsAsync(ISchedulerFactory schedulerFactory, IPrincipalUtils principalUtils, ClaimsPrincipal user, QueryValueBag query)
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