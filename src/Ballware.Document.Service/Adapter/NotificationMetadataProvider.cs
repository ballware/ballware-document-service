using Ballware.Document.Data.Repository;
using Ballware.Document.Metadata;

namespace Ballware.Document.Service.Adapter;

public class NotificationMetadataProvider : INotificationMetadataProvider
{
    private INotificationMetaRepository Repository { get; }
    
    public NotificationMetadataProvider(INotificationMetaRepository repository)
    {
        Repository = repository;   
    }
    
    public async Task<Data.Public.Notification?> NotificationForTenantAndIdAsync(Guid tenantId, Guid notificationId)
    {
        return await Repository.MetadataByTenantAndIdAsync(tenantId, notificationId);
    }
}