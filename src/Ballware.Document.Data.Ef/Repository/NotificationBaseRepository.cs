using AutoMapper;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Ballware.Shared.Data.Ef.Repository;
using Ballware.Shared.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Ballware.Document.Data.Ef.Repository;

public abstract class NotificationBaseRepository : TenantableBaseRepository<Public.Notification, Persistables.Notification>, INotificationMetaRepository
{
    private IDocumentDbContext DocumentDbContext { get; }

    public NotificationBaseRepository(IMapper mapper, IDocumentDbContext dbContext,
        ITenantableRepositoryHook<Public.Notification, Persistables.Notification>? hook = null)
        : base(mapper, dbContext, hook)
    {
        DocumentDbContext = dbContext;
    }

    public async Task<Public.Notification?> MetadataByTenantAndIdAsync(Guid tenantId, Guid id)
    {
        var result = await DocumentDbContext.Notifications.SingleOrDefaultAsync(c => c.TenantId == tenantId && c.Uuid == id);

        return result != null ? Mapper.Map<Public.Notification>(result) : null;
    }

    public async Task<Public.Notification?> MetadataByTenantAndIdentifierAsync(Guid tenantId, string identifier)
    {
        var result = await DocumentDbContext.Notifications.SingleOrDefaultAsync(c => c.TenantId == tenantId && c.Identifier == identifier);

        return result != null ? Mapper.Map<Public.Notification>(result) : null;
    }
    
    public virtual async Task<IEnumerable<NotificationSelectListEntry>> SelectListForTenantAsync(Guid tenantId)
    {
        return await Task.FromResult(DocumentDbContext.Notifications.Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Identifier)
            .Select(r => new NotificationSelectListEntry
                { Id = r.Uuid, Name = r.Name }));
    }
    
    public virtual async Task<NotificationSelectListEntry?> SelectByIdForTenantAsync(Guid tenantId, Guid id)
    {
        return await DocumentDbContext.Notifications.Where(r => r.TenantId == tenantId && r.Uuid == id)
            .Select(r => new NotificationSelectListEntry
                { Id = r.Uuid, Name = r.Name })
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetCurrentStateForTenantAndIdAsync(Guid tenantId, Guid id)
    {
        return await DocumentDbContext.Notifications
            .Where(d => d.TenantId == tenantId && d.Uuid == id)
            .Select(d => (int?)d.State)
            .FirstOrDefaultAsync();
    }

    public abstract Task<string> GenerateListQueryAsync(Guid tenantId);
}
