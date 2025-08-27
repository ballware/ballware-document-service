using AutoMapper;
using Ballware.Document.Data.Ef.Repository;
using Ballware.Shared.Data.Repository;

namespace Ballware.Document.Data.Ef.SqlServer.Repository;

public class NotificationRepository : NotificationBaseRepository
{
    public NotificationRepository(IMapper mapper, IDocumentDbContext dbContext,
        ITenantableRepositoryHook<Public.Notification, Persistables.Notification>? hook = null)
        : base(mapper, dbContext, hook)
    {
    }

    public override Task<string> GenerateListQueryAsync(Guid tenantId)
    {
        return Task.FromResult($"SELECT uuid AS Id, name as Name FROM notification WHERE tenant_id='{tenantId}'");
    }
}
