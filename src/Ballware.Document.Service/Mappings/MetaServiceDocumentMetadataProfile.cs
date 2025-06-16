using AutoMapper;
using Ballware.Document.Metadata;

namespace Ballware.Document.Service.Mappings;

public class MetaServiceDocumentMetadataProfile : Profile
{
    public MetaServiceDocumentMetadataProfile()
    {
        CreateMap<Ballware.Meta.Client.ServiceTenantReportDatasourceDefinition, ReportDatasourceDefinition>();
        CreateMap<Ballware.Meta.Client.ServiceTenantReportDatasourceTable, ReportDatasourceTable>();
    }
}