namespace Ballware.Document.Metadata;

public interface IDocumentLookupProvider
{
    string LookupColumnValueByTenantAndId(Guid tenantId, Guid lookupId, string id, string column);
}