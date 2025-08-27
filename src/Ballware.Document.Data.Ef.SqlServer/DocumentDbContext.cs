using Ballware.Document.Data.Persistables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ballware.Document.Data.Ef.SqlServer;

public class DocumentDbContext : DbContext, IDocumentDbContext
{
    private ILoggerFactory LoggerFactory { get; }
    
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options, ILoggerFactory loggerFactory) : base(options)
    {
        LoggerFactory = loggerFactory;
    }

    public DbSet<Persistables.Document> Documents { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLoggerFactory(LoggerFactory);
        
        base.OnConfiguring(optionsBuilder);
    }
}
