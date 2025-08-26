namespace Ballware.Document.Metadata;

public interface IMetaDatasourceProvider
{
    IDictionary<string, object> LookupMetadataForTenantDatasourceAndIdentifier(Guid tenantId, string datasourceName, string identifier);
}