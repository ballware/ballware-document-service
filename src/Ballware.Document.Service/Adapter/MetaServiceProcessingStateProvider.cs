using Ballware.Document.Metadata;
using Ballware.Meta.Client;

namespace Ballware.Document.Service.Adapter;

public class MetaServiceProcessingStateProvider : IDocumentProcessingStateProvider
{
    private BallwareMetaClient MetaClient { get; }
    
    public MetaServiceProcessingStateProvider(BallwareMetaClient metaClient)
    {
        MetaClient = metaClient;
    }
    
    public string ProcessingStateNameForTenantAndEntityAndState(Guid tenantId, string entity, int state)
    {
        var processingState = MetaClient.ProcessingStateSelectByStateForTenantAndEntityByIdentifier(tenantId, entity, state);
        
        return processingState?.Name ?? string.Empty;
    }
}