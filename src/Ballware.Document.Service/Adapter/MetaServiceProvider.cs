using AutoMapper;
using Ballware.Document.Api;
using Ballware.Document.Data;
using Ballware.Document.Data.Repository;
using Ballware.Document.Metadata;
using Ballware.Meta.Service.Client;
using Ballware.Shared.Api;
using Ballware.Shared.Authorization;

namespace Ballware.Document.Service.Adapter;

public class MetaServiceProvider : IAuthorizationMetadataProvider, IDatasourceDefinitionProvider, IMetaDatasourceProvider, IExportMetadataProvider, IJobMetadataProvider, IDocumentPickvalueProvider, IDocumentProcessingStateProvider, IProcessingStateProvider
{
    private const string DocumentLookupsDatasourceIdentifier = "DocumentLookups";
    
    private IMapper Mapper { get; }
    private MetaServiceClient MetaClient { get; }
    
    private IDocumentDbConnectionFactory DocumentDbConnectionFactory { get; }
    private IDocumentMetaRepository DocumentMetaRepository { get; }
    private INotificationMetaRepository NotificationMetaRepository { get; }
    private ISubscriptionMetaRepository SubscriptionMetaRepository { get; }
    
    public MetaServiceProvider(IMapper mapper, IDocumentDbConnectionFactory documentDbConnectionFactory, IDocumentMetaRepository documentMetaRepository, INotificationMetaRepository notificationMetaRepository, ISubscriptionMetaRepository subscriptionMetaRepository, MetaServiceClient metaClient)
    {
        Mapper = mapper;
        DocumentDbConnectionFactory = documentDbConnectionFactory;   
        DocumentMetaRepository = documentMetaRepository;
        NotificationMetaRepository = notificationMetaRepository;
        SubscriptionMetaRepository = subscriptionMetaRepository;
        MetaClient = metaClient;
    }   
    
    public async Task<ITenantAuthorizationMetadata?> MetadataForTenantByIdAsync(Guid tenantId)
    {
        var tenant = await MetaClient.TenantServiceMetadataAsync(tenantId);
        
        return Mapper.Map<Tenant>(tenant);   
    }

    public async Task<IEntityAuthorizationMetadata?> MetadataForEntityByTenantAndIdentifierAsync(Guid tenantId, string entity)
    {
        var entityMetadata = await MetaClient.EntityServiceMetadataForTenantByIdentifierAsync(tenantId, entity);
        
        return Mapper.Map<EntityMetadata>(entityMetadata);  
    }
    
    public IEnumerable<ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId)
    {
        var documentServiceDataSourceDefinitions = new List<ReportDatasourceDefinition>();
        
        var documentLookupsSchemaDefinition = new ReportDatasourceDefinition
        {
            Name = DocumentLookupsDatasourceIdentifier,
            Provider = DocumentDbConnectionFactory.Provider,
            ConnectionString = DocumentDbConnectionFactory.ConnectionString,
            Tables = new []
                {
                    new ReportDatasourceTable { Name = "documentLookup", Query = DocumentMetaRepository.GenerateListQueryAsync(tenantId).GetAwaiter().GetResult() },
                    new ReportDatasourceTable { Name = "notificationLookup", Query = NotificationMetaRepository.GenerateListQueryAsync(tenantId).GetAwaiter().GetResult() },
                    new ReportDatasourceTable { Name = "subscriptionLookup", Query = SubscriptionMetaRepository.GenerateListQueryAsync(tenantId).GetAwaiter().GetResult() },
                }
        };
        
        documentServiceDataSourceDefinitions.Add(documentLookupsSchemaDefinition);
        
        var metaServiceDataSourceDefinitions =  Mapper.Map<IEnumerable<ReportDatasourceDefinition>>(MetaClient.TenantReportMetaDatasources(tenantId));
        
        return documentServiceDataSourceDefinitions.Concat(metaServiceDataSourceDefinitions);
    }

    public IDictionary<string, object> LookupMetadataForTenantDatasourceAndIdentifier(Guid tenantId, string datasourceName, string identifier)
    {
        return MetaClient.TenantReportLookupMetadataForTenantAndLookup(tenantId, datasourceName, identifier);
    }
    
    public async Task<Guid> CreateExportAsync(Guid tenantId, Guid userId, Shared.Api.Public.Export payload)
    {
        return await MetaClient.ExportCreateForTenantBehalfOfUserAsync(tenantId, userId, new ExportCreatePayload()
        {
            Application = payload.Application,
            Entity = payload.Entity,
            Query = payload.Query,
            ExpirationStamp = payload.ExpirationStamp,
            MediaType = payload.MediaType
        });
    }

    public async Task<Shared.Api.Public.Export?> GetExportByIdAsync(Guid tenantId, Guid exportId)
    {
        var export = await MetaClient.ExportFetchForTenantByIdAsync(tenantId, exportId);

        return new Shared.Api.Public.Export()
        {
            Id = export.Id,
            Application = export.Application,
            Entity = export.Entity,
            Query = export.Query,
            ExpirationStamp = export.ExpirationStamp?.DateTime,
            MediaType = export.MediaType,
        };
    }
    
    public async Task<Guid> CreateJobAsync(Guid tenantId, Guid userId, string scheduler, string identifier, string? options)
    {
        var job = await MetaClient.JobCreateForTenantBehalfOfUserAsync(tenantId, userId, new JobCreatePayload()
        {
            Scheduler = scheduler,
            Identifier = identifier,
            Options = options,
        });

        if (job == null)
        {
            throw new InvalidOperationException($"Failed to create job for tenant {tenantId}.");
        }
        
        return job.Id;
    }

    public async Task UpdateJobAsync(Guid tenantId, Guid userId, Guid id, Ballware.Shared.Api.Public.JobStates state, string? result)
    {
        await MetaClient.JobUpdateForTenantBehalfOfUserAsync(tenantId, userId, new JobUpdatePayload()
        {
            Id = id,
            State = Mapper.Map<Ballware.Meta.Service.Client.JobStates>(state),
            Result = result
        });
    }
    
    public string PickvalueNameForTenantAndEntityAndFieldByValue(Guid tenantId, string entity, string field, int value)
    {
        var pickvalue = MetaClient.PickvalueSelectByValueForTenantEntityAndField(tenantId, entity, field, value);
        
        return pickvalue?.Name ?? string.Empty;
    }
    
    public string ProcessingStateNameForTenantAndEntityAndState(Guid tenantId, string entity, int state)
    {
        var processingState = MetaClient.ProcessingStateSelectByStateForTenantAndEntityByIdentifier(tenantId, entity, state);
        
        return processingState?.Name ?? string.Empty;
    }

    public async Task<IEnumerable<Metadata.ProcessingStateSelectListEntry>> SelectListPossibleSuccessorsForEntityAsync(Guid tenantId, string entity, int state)
    {
        var possibleSuccessors = await MetaClient.ProcessingStateSelectListAllSuccessorsForTenantAndEntityByIdentifierAsync(tenantId, entity, state);

        if (possibleSuccessors == null)
        {
            return [];
        }
        
        return Mapper.Map<IEnumerable<Metadata.ProcessingStateSelectListEntry>>(possibleSuccessors);   
    }
}