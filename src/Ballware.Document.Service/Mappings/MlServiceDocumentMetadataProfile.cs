using AutoMapper;
using Ballware.Document.Metadata;

namespace Ballware.Document.Service.Mappings;

public class MlServiceDocumentMetadataProfile : Profile
{
    public MlServiceDocumentMetadataProfile()
    {
        CreateMap<Ballware.Ml.Service.Client.ReportDatasourceDefinition, ReportDatasourceDefinition>();
        CreateMap<Ballware.Ml.Service.Client.ReportDatasourceTable, ReportDatasourceTable>();
        CreateMap<Ballware.Ml.Service.Client.ReportDatasourceRelation, ReportDatasourceRelation>();
    }
}