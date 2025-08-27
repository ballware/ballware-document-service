using AutoMapper;
using Ballware.Document.Metadata;
using Ballware.Generic.Service.Client;

namespace Ballware.Document.Service.Adapter;

public class GenericServiceProvider : IDatasourceDefinitionProvider, IDocumentLookupProvider
{
    private IMapper Mapper { get; }
    private GenericServiceClient GenericClient { get; }
    
    public GenericServiceProvider(IMapper mapper, GenericServiceClient genericClient)
    {
        Mapper = mapper;
        GenericClient = genericClient;
    }
    
    public IEnumerable<Metadata.ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId)
    {
        return Mapper.Map<IEnumerable<Metadata.ReportDatasourceDefinition>>(GenericClient.TenantReportDatasources(tenantId));
    }
    
    public string LookupColumnValueByTenantAndId(Guid tenantId, Guid lookupId, string id, string column)
    {
        var result = GenericClient.LookupSelectColumnValueByIdForTenantAndLookupId(tenantId, lookupId, id, column) as string;
        
        return result ?? string.Empty;
    }
}