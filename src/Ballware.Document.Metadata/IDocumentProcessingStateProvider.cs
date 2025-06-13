namespace Ballware.Document.Metadata;

public interface IDocumentProcessingStateProvider
{
    string ProcessingStateNameForTenantAndEntityAndState(Guid tenantId, string entity, int state);
}