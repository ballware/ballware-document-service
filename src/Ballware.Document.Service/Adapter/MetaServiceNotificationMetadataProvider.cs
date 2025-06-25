using AutoMapper;
using Ballware.Document.Metadata;
using Ballware.Meta.Client;
using Notification = Ballware.Document.Metadata.Notification;

namespace Ballware.Document.Service.Adapter;

public class MetaServiceNotificationMetadataProvider : INotificationMetadataProvider
{
    private IMapper Mapper { get; }
    private BallwareMetaClient MetaClient { get; }
    
    public MetaServiceNotificationMetadataProvider(IMapper mapper, BallwareMetaClient metaClient)
    {
        Mapper = mapper;
        MetaClient = metaClient;
    }
    
    public async Task<Notification?> NotificationForTenantAndIdAsync(Guid tenantId, Guid notificationId)
    {
        return Mapper.Map<Notification?>(await MetaClient.NotificationMetadataByTenantAndIdAsync(tenantId, notificationId));
    }
}