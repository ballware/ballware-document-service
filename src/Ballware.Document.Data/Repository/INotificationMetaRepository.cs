using Ballware.Document.Data.SelectLists;
using Ballware.Shared.Data.Repository;

namespace Ballware.Document.Data.Repository;

public interface INotificationMetaRepository : ITenantableRepository<Public.Notification>
{
    Task<Public.Notification?> MetadataByTenantAndIdAsync(Guid tenantId, Guid id);
    Task<Public.Notification?> MetadataByTenantAndIdentifierAsync(Guid tenantId, string identifier);
    
    Task<IEnumerable<NotificationSelectListEntry>> SelectListForTenantAsync(Guid tenantId);
    Task<NotificationSelectListEntry?> SelectByIdForTenantAsync(Guid tenantId, Guid id);
    
    Task<int?> GetCurrentStateForTenantAndIdAsync(Guid tenantId, Guid id);
    
    Task<string> GenerateListQueryAsync(Guid tenantId);
}