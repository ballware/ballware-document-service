using AutoMapper;
using Ballware.Document.Metadata;
using Ballware.Meta.Client;
using Notification = Ballware.Document.Metadata.Notification;
using Subscription = Ballware.Document.Metadata.Subscription;

namespace Ballware.Document.Service.Adapter;

public class MetaServiceSubscriptionMetadataProvider : ISubscriptionMetadataProvider
{
    private IMapper Mapper { get; }
    private BallwareMetaClient MetaClient { get; }
    
    public MetaServiceSubscriptionMetadataProvider(IMapper mapper, BallwareMetaClient metaClient)
    {
        Mapper = mapper;
        MetaClient = metaClient;
    }

    public async Task<Subscription?> SubscriptionForTenantAndIdAsync(Guid tenantId, Guid subscriptionId)
    {
        return Mapper.Map<Subscription?>(await MetaClient.SubscriptionMetadataByTenantAndIdAsync(tenantId, subscriptionId));
    }

    public async Task SetSendResultForSubscriptionAsync(Guid tenantId, Guid subscriptionId, string? sendResult)
    {
        await MetaClient.SubscriptionSetSendResultAsync(tenantId, subscriptionId, sendResult ?? string.Empty);
    }
}