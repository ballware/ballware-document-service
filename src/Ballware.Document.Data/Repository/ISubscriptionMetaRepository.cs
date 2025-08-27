using Ballware.Document.Data.SelectLists;
using Ballware.Shared.Data.Repository;

namespace Ballware.Document.Data.Repository;

public interface ISubscriptionMetaRepository : ITenantableRepository<Public.Subscription>
{
    Task<Public.Subscription?> MetadataByTenantAndIdAsync(Guid tenantId, Guid id);

    Task<IEnumerable<Public.Subscription>> GetActiveSubscriptionsByTenantAndFrequencyAsync(Guid tenantId, int frequency);

    Task SetLastErrorAsync(Guid tenantId, Guid id, string message);
    
    Task<IEnumerable<SubscriptionSelectListEntry>> SelectListForTenantAsync(Guid tenantId);
    Task<SubscriptionSelectListEntry?> SelectByIdForTenantAsync(Guid tenantId, Guid id);
    
    Task<string> GenerateListQueryAsync(Guid tenantId);
}