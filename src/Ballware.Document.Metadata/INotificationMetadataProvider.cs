namespace Ballware.Document.Metadata;

public interface INotificationMetadataProvider
{
    Task<Notification?> NotificationForTenantAndIdAsync(Guid tenantId, Guid notificationId); 
}