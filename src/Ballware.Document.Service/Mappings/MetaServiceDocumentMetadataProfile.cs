using AutoMapper;
using Ballware.Document.Metadata;

namespace Ballware.Document.Service.Mappings;

public class MetaServiceDocumentMetadataProfile : Profile
{
    public MetaServiceDocumentMetadataProfile()
    {
        CreateMap<Ballware.Meta.Service.Client.ServiceTenantReportDatasourceDefinition, ReportDatasourceDefinition>();
        CreateMap<Ballware.Meta.Service.Client.ServiceTenantReportDatasourceTable, ReportDatasourceTable>();

        CreateMap<Ballware.Meta.Service.Client.ServiceEntity, EntityMetadata>()
            .ForMember(dst => dst.RightsCheckScript, 
                opt => opt.MapFrom(source => source.CustomScripts.ExtendedRightsCheck));
        
        CreateMap<Ballware.Meta.Service.Client.ServiceTenant, Tenant>();
        CreateMap<Ballware.Meta.Service.Client.TenantSelectListEntry, TenantListEntry>();

        CreateMap<Ballware.Shared.Api.Public.JobStates, Ballware.Meta.Service.Client.JobStates>();
        CreateMap<Ballware.Meta.Service.Client.JobStates, Ballware.Shared.Api.Public.JobStates>();
        
        CreateMap<Ballware.Meta.Service.Client.ProcessingStateSelectListEntry, ProcessingStateSelectListEntry>();
    }
}