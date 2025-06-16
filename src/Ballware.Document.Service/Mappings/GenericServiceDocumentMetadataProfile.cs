using AutoMapper;
using Ballware.Document.Metadata;

namespace Ballware.Document.Service.Mappings;

public class GenericServiceDocumentMetadataProfile : Profile
{
    public GenericServiceDocumentMetadataProfile()
    {
        CreateMap<Ballware.Generic.Client.ReportDatasourceDefinition, ReportDatasourceDefinition>();
        CreateMap<Ballware.Generic.Client.ReportDatasourceTable, ReportDatasourceTable>();
        CreateMap<Ballware.Generic.Client.ReportDatasourceRelation, ReportDatasourceRelation>();
    }
}