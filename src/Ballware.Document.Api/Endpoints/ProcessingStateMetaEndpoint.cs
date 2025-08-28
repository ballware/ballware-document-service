using System.Security.Claims;
using Ballware.Document.Data.Repository;
using Ballware.Document.Metadata;
using Ballware.Shared.Api.Endpoints.Bindings;
using Ballware.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ballware.Document.Api.Endpoints;

public static class ProcessingStateMetaEndpoint
{
    private const string ApiTag = "ProcessingState";
    private const string ApiOperationPrefix = "ProcessingState";
    
    public static IEndpointRouteBuilder MapProcessingStateDocumentApi(this IEndpointRouteBuilder app, 
        string basePath,
        string apiTag = ApiTag,
        string apiOperationPrefix = ApiOperationPrefix,
        string authorizationScope = "documentApi")
    {
        app.MapGet(basePath + "/selectlistallowedsuccessorsforentities/document", HandleSelectListAllowedSuccessorsForDocumentByIdsAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<IEnumerable<ProcessingStateSelectListEntry>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "SelectListAllSuccessorsForDocumentByIds")
            .WithGroupName("document")
            .WithTags(apiTag)
            .WithSummary("Query all possible successor processing states for documents by instance ids");
        
        app.MapGet(basePath + "/selectlistallowedsuccessorsforentities/notification", HandleSelectListAllowedSuccessorsForNotificationByIdsAsync)
            .RequireAuthorization(authorizationScope)
            .Produces<IEnumerable<ProcessingStateSelectListEntry>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(apiOperationPrefix + "SelectListAllSuccessorsForNotificationByIds")
            .WithGroupName("notification")
            .WithTags(apiTag)
            .WithSummary("Query all possible successor processing states for notifications by instance ids");
        
        return app;
    }
    
    private static async Task<IResult> HandleSelectListAllowedSuccessorsForDocumentByIdsAsync(IPrincipalUtils principalUtils, IEntityRightsChecker entityRightsChecker, IAuthorizationMetadataProvider authorizationMetadataProvider, IProcessingStateProvider processingStateProvider, IDocumentMetaRepository documentMetaRepository, ClaimsPrincipal user, QueryValueBag query)
    {
        var tenantId = principalUtils.GetUserTenandId(user);
        var rights = principalUtils.GetUserRights(user);

        var entityMeta = await authorizationMetadataProvider.MetadataForEntityByTenantAndIdentifierAsync(tenantId, "document");

        if (entityMeta == null)
        {
            return Results.NotFound($"Entity document not found.");
        }

        query.Query.TryGetValue("id", out var ids);
        
        var listOfStates = new List<IEnumerable<ProcessingStateSelectListEntry>>();

        foreach (var id in ids.Select(Guid.Parse))
        {
            var currentState = await documentMetaRepository.GetCurrentStateForTenantAndIdAsync(tenantId, id);
            var possibleStates = currentState != null
                ? (await processingStateProvider.SelectListPossibleSuccessorsForEntityAsync(tenantId, "document",
                    currentState.Value)).ToList()
                : [];
            var allowedStates = possibleStates?.Where(ps => entityRightsChecker.StateAllowedAsync(tenantId, entityMeta, id, ps.State, rights).GetAwaiter().GetResult());

            listOfStates.Add(allowedStates);
        }

        if (listOfStates.Count > 1)
        {
            return Results.Ok(listOfStates.Skip(1).Aggregate(new HashSet<ProcessingStateSelectListEntry>(listOfStates[0]), (h, e) =>
            {
                h.IntersectWith(e);
                return h;
            }));
        }
        
        if (listOfStates.Count == 1)
        {
            return Results.Ok(listOfStates[0]);
        }
        
        return Results.Ok(new List<ProcessingStateSelectListEntry>());
    }
    
    private static async Task<IResult> HandleSelectListAllowedSuccessorsForNotificationByIdsAsync(IPrincipalUtils principalUtils, IEntityRightsChecker entityRightsChecker, IAuthorizationMetadataProvider authorizationMetadataProvider, IProcessingStateProvider processingStateProvider, INotificationMetaRepository notificationMetaRepository, ClaimsPrincipal user, QueryValueBag query)
    {
        var tenantId = principalUtils.GetUserTenandId(user);
        var rights = principalUtils.GetUserRights(user);

        var entityMeta =
            await authorizationMetadataProvider.MetadataForEntityByTenantAndIdentifierAsync(tenantId, "notification");

        if (entityMeta == null)
        {
            return Results.NotFound($"Entity notification not found.");
        }

        query.Query.TryGetValue("id", out var ids);
        
        var listOfStates = (await Task.WhenAll(ids.Select(Guid.Parse).Select(async (id) =>
        {
            var currentState = await notificationMetaRepository.GetCurrentStateForTenantAndIdAsync(tenantId, id);
            var possibleStates = currentState != null
                ? await processingStateProvider.SelectListPossibleSuccessorsForEntityAsync(tenantId, "notification",
                    currentState.Value)
                : [];
            var allowedStates = possibleStates?.Where(ps => entityRightsChecker.StateAllowedAsync(tenantId, entityMeta, id, ps.State, rights).GetAwaiter().GetResult());

            return allowedStates;
        })))?.ToList();

        if (listOfStates != null && listOfStates.Count > 1)
        {
            return Results.Ok(listOfStates.Skip(1).Aggregate(new HashSet<ProcessingStateSelectListEntry>(listOfStates[0]), (h, e) =>
            {
                h.IntersectWith(e);
                return h;
            }));
        }
        
        if (listOfStates != null && listOfStates.Count == 1)
        {
            return Results.Ok(listOfStates[0]);
        }
        
        return Results.Ok(new List<ProcessingStateSelectListEntry>());
    }
}