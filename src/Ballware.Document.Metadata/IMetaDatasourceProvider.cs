namespace Ballware.Document.Metadata;

public interface IMetaDatasourceProvider
{
    IEnumerable<ReportDatasourceDefinition> DatasourceDefinitionsForTenant(Guid tenantId);
    IDictionary<string, object> LookupMetadataForTenantDatasourceAndIdentifier(Guid tenantId, string datasourceName, string identifier);
}