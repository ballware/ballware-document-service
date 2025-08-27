using AutoMapper;
using Ballware.Document.Metadata;

namespace Ballware.Document.Service.Mappings;

public class GenericServiceDocumentMetadataProfile : Profile
{
    public GenericServiceDocumentMetadataProfile()
    {
        CreateMap<Ballware.Generic.Service.Client.ReportDatasourceDefinition, ReportDatasourceDefinition>();
        CreateMap<Ballware.Generic.Service.Client.ReportDatasourceTable, ReportDatasourceTable>();
        CreateMap<Ballware.Generic.Service.Client.ReportDatasourceRelation, ReportDatasourceRelation>();
    }
}