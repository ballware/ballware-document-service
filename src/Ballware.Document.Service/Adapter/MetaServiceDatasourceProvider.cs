using AutoMapper;
using Ballware.Document.Metadata;
using Ballware.Meta.Client;

namespace Ballware.Document.Service.Adapter;

public class MetaServiceDatasourceProvider : IMetaDatasourceProvider
{
    private IMapper Mapper { get; }
    private BallwareMetaClient MetaClient { get; }
    
    public MetaServiceDatasourceProvider(IMapper mapper, BallwareMetaClient metaClient)
    {
        Mapper = mapper;
        MetaClient = metaClient;
    }

    public IEnumerable<ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId)
    {
        return Mapper.Map<IEnumerable<ReportDatasourceDefinition>>(MetaClient.TenantReportMetaDatasources(tenantId));
    }
}