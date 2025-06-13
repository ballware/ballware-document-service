namespace Ballware.Document.Metadata;

public interface IDocumentPickvalueProvider
{
    string PickvalueNameForTenantAndEntityAndFieldByValue(Guid tenantId, string entity, string field, int value);
}