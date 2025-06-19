using Ballware.Document.Metadata;
using Ballware.Meta.Client;

namespace Ballware.Document.Service.Adapter;

public class MetaServicePickvalueProvider : IDocumentPickvalueProvider
{
    private BallwareMetaClient MetaClient { get; }
    
    public MetaServicePickvalueProvider(BallwareMetaClient metaClient)
    {
        MetaClient = metaClient;
    }

    public string PickvalueNameForTenantAndEntityAndFieldByValue(Guid tenantId, string entity, string field, int value)
    {
        var pickvalue = MetaClient.PickvalueSelectByValueForTenantEntityAndField(tenantId, entity, field, value);
        
        return pickvalue?.Name ?? string.Empty;
    }
}