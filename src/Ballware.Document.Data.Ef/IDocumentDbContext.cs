using Ballware.Document.Data.Persistables;
using Ballware.Shared.Data.Ef;
using Microsoft.EntityFrameworkCore;

namespace Ballware.Document.Data.Ef;

public interface IDocumentDbContext : IDbContext
{
    DbSet<Persistables.Document> Documents { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Subscription> Subscriptions { get; }
}