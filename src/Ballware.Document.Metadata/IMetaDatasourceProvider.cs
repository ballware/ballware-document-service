namespace Ballware.Document.Metadata;

public interface IMetaDatasourceProvider
{
    IEnumerable<ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId);
}