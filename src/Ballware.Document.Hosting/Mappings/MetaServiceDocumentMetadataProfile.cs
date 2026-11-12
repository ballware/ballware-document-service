using Mapster;
using Ballware.Document.Metadata;

namespace Ballware.Document.Hosting.Mappings;

public class MetaServiceDocumentMetadataProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ballware.Meta.Service.Client.ServiceTenantReportDatasourceDefinition, ReportDatasourceDefinition>();
        config.NewConfig<Ballware.Meta.Service.Client.ServiceTenantReportDatasourceTable, ReportDatasourceTable>();

        config.NewConfig<Ballware.Meta.Service.Client.ServiceEntity, EntityMetadata>()
            .Map(dst => dst.RightsCheckScript,
                src => src.CustomScripts.ExtendedRightsCheck);

        config.NewConfig<Ballware.Meta.Service.Client.ServiceTenant, Tenant>();
        config.NewConfig<Ballware.Meta.Service.Client.TenantSelectListEntry, TenantListEntry>();

        config.NewConfig<Ballware.Shared.Api.Public.JobStates, Ballware.Meta.Service.Client.JobStates>();
        config.NewConfig<Ballware.Meta.Service.Client.JobStates, Ballware.Shared.Api.Public.JobStates>();

        config.NewConfig<Ballware.Meta.Service.Client.ProcessingStateSelectListEntry, ProcessingStateSelectListEntry>();
    }
}
