namespace Ballware.Document.Metadata;

public interface IDatasourceDefinitionProvider
{
    IEnumerable<ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId);
}