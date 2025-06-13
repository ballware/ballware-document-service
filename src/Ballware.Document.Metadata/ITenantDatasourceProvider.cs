namespace Ballware.Document.Metadata;

public interface ITenantDatasourceProvider
{
    IEnumerable<ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId);
}