using AutoMapper;
using Ballware.Document.Metadata;
using Ballware.Ml.Service.Client;

namespace Ballware.Document.Service.Adapter;

public class MlServiceProvider : IDatasourceDefinitionProvider
{
    private IMapper Mapper { get; }
    private MlServiceClient MlClient { get; }
    
    public MlServiceProvider(IMapper mapper, MlServiceClient mlClient)
    {
        Mapper = mapper;
        MlClient = mlClient;
    }
    
    public IEnumerable<Metadata.ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId)
    {
        return Mapper.Map<IEnumerable<Metadata.ReportDatasourceDefinition>>(MlClient.TenantReportMlDatasources(tenantId));
    }
}