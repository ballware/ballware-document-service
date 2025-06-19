using Ballware.Document.Metadata;
using Ballware.Generic.Client;

namespace Ballware.Document.Service.Adapter;

public class GenericServiceLookupProvider : IDocumentLookupProvider
{
    private BallwareGenericClient GenericClient { get; }
    
    public GenericServiceLookupProvider(BallwareGenericClient genericClient)
    {
        GenericClient = genericClient;
    }

    public string LookupColumnValueByTenantAndId(Guid tenantId, Guid lookupId, string id, string column)
    {
        var result = GenericClient.LookupSelectColumnValueByIdForTenantAndLookupId(tenantId, lookupId, id, column) as string;
        
        return result ?? string.Empty;
    }
}