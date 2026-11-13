using Mapster;
using Ballware.Document.Metadata;

namespace Ballware.Document.Hosting.Mappings;

public class GenericServiceDocumentMetadataProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ballware.Generic.Service.Client.ReportDatasourceDefinition, ReportDatasourceDefinition>();
        config.NewConfig<Ballware.Generic.Service.Client.ReportDatasourceTable, ReportDatasourceTable>();
        config.NewConfig<Ballware.Generic.Service.Client.ReportDatasourceRelation, ReportDatasourceRelation>();
    }
}
