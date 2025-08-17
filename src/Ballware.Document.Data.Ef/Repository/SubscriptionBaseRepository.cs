using AutoMapper;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Ballware.Shared.Data.Ef.Repository;
using Ballware.Shared.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Ballware.Document.Data.Ef.Repository;

public abstract class SubscriptionBaseRepository : TenantableBaseRepository<Public.Subscription, Persistables.Subscription>, ISubscriptionMetaRepository
{
    private IDocumentDbContext DocumentDbContext { get; }

    public SubscriptionBaseRepository(IMapper mapper, IDocumentDbContext dbContext,
        ITenantableRepositoryHook<Public.Subscription, Persistables.Subscription>? hook = null)
        : base(mapper, dbContext, hook)
    {
        DocumentDbContext = dbContext;
    }

    public async Task<Public.Subscription?> MetadataByTenantAndIdAsync(Guid tenantId, Guid id)
    {
        var result = await DocumentDbContext.Subscriptions.SingleOrDefaultAsync(c => c.TenantId == tenantId && c.Uuid == id);

        return result != null ? Mapper.Map<Public.Subscription>(result) : null;
    }

    public virtual async Task<IEnumerable<Public.Subscription>> GetActiveSubscriptionsByTenantAndFrequencyAsync(Guid tenantId, int frequency)
    {
        return await Task.Run(() => DocumentDbContext.Subscriptions.Where(s => s.TenantId == tenantId && s.Active && s.Frequency == frequency).Select(s => Mapper.Map<Public.Subscription>(s)));
    }

    public virtual async Task SetLastErrorAsync(Guid tenantId, Guid id, string message)
    {
        var subscription = await DocumentDbContext.Subscriptions.SingleAsync(c => c.TenantId == tenantId && c.Uuid == id);

        subscription.LastSendStamp = DateTime.Now;
        subscription.LastError = message ?? "OK";

        DocumentDbContext.Subscriptions.Update(subscription);

        await Context.SaveChangesAsync();
    }
    
    public virtual async Task<IEnumerable<SubscriptionSelectListEntry>> SelectListForTenantAsync(Guid tenantId)
    {
        return await Task.FromResult(DocumentDbContext.Subscriptions
            .Where(p => p.TenantId == tenantId)
            .Select(d => new SubscriptionSelectListEntry { Id = d.Uuid, NotificationId = d.NotificationId, UserId = d.UserId, Active = d.Active }));
    }
    
    public virtual async Task<SubscriptionSelectListEntry?> SelectByIdForTenantAsync(Guid tenantId, Guid id)
    {
        return await DocumentDbContext.Subscriptions.Where(r => r.TenantId == tenantId && r.Uuid == id)
            .Select(d => new SubscriptionSelectListEntry { Id = d.Uuid, NotificationId = d.NotificationId, UserId = d.UserId, Active = d.Active })
            .FirstOrDefaultAsync();
    }

    public abstract Task<string> GenerateListQueryAsync(Guid tenantId);
}