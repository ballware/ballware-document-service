using Ballware.Document.Data.Persistables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ballware.Document.Data.Ef.Model;

public class DocumentModelBaseCustomizer : RelationalModelCustomizer
{
    public DocumentModelBaseCustomizer(ModelCustomizerDependencies dependencies) 
        : base(dependencies)
    {
    }
    
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }

                if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new ValueConverter<DateTime?, DateTime?>(
                        v => v.HasValue ? v.Value.ToUniversalTime() : v,
                        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v));
                }
            }
        }
        
        modelBuilder.Entity<Persistables.Document>().ToTable("document");
        modelBuilder.Entity<Notification>().ToTable("notification");
        modelBuilder.Entity<Subscription>().ToTable("subscription");
        
        modelBuilder.Entity<Persistables.Document>().HasKey(d => d.Id);
        modelBuilder.Entity<Persistables.Document>().HasIndex(d => new { d.TenantId, d.Uuid }).IsUnique();
        modelBuilder.Entity<Persistables.Document>().HasIndex(d => d.TenantId);

        modelBuilder.Entity<Notification>().HasKey(d => d.Id);
        modelBuilder.Entity<Notification>().HasIndex(d => new { d.TenantId, d.Uuid }).IsUnique();
        modelBuilder.Entity<Notification>().HasIndex(d => d.TenantId);
        modelBuilder.Entity<Notification>().HasIndex(d => new { d.TenantId, d.Identifier }).IsUnique();

        modelBuilder.Entity<Subscription>().HasKey(d => d.Id);
        modelBuilder.Entity<Subscription>().HasIndex(d => new { d.TenantId, d.Uuid }).IsUnique();
        modelBuilder.Entity<Subscription>().HasIndex(d => d.TenantId);
        modelBuilder.Entity<Subscription>().HasIndex(d => d.Frequency);
    }
}