using Mapster;
using Ballware.Document.Metadata;

namespace Ballware.Document.Service.Mappings;

public class MlServiceDocumentMetadataProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ballware.Ml.Service.Client.ReportDatasourceDefinition, ReportDatasourceDefinition>();
        config.NewConfig<Ballware.Ml.Service.Client.ReportDatasourceTable, ReportDatasourceTable>();
        config.NewConfig<Ballware.Ml.Service.Client.ReportDatasourceRelation, ReportDatasourceRelation>();
    }
}
