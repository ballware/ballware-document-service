using AutoMapper;
using Ballware.Document.Metadata;
using Ballware.Generic.Client;
using ReportDatasourceDefinition = Ballware.Document.Metadata.ReportDatasourceDefinition;

namespace Ballware.Document.Service.Adapter;

public class GenericServiceDatasourceProvider : ITenantDatasourceProvider
{
    private IMapper Mapper { get; }
    private BallwareGenericClient GenericClient { get; }
    
    public GenericServiceDatasourceProvider(IMapper mapper, BallwareGenericClient genericClient)
    {
        Mapper = mapper;
        GenericClient = genericClient;
    }
    
    public IEnumerable<ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId)
    {
        return Mapper.Map<IEnumerable<ReportDatasourceDefinition>>(GenericClient.TenantReportDatasources(tenantId));
    }
}