using Ballware.Shared.Data.Repository;
using Ballware.Document.Data.Ef.Configuration;
using Ballware.Document.Data.Ef.Model;
using Ballware.Document.Data.Ef.SqlServer.Internal;
using Ballware.Document.Data.Ef.SqlServer.Repository;
using Ballware.Document.Data.Public;
using Ballware.Document.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Data.Ef.SqlServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareDocumentStorageForSqlServer(this IServiceCollection services, StorageOptions options, string connectionString)
    {
        services.AddSingleton(options);
        services.AddDbContext<DocumentDbContext>(o =>
        {
            o.UseSqlServer(connectionString, o =>
            {
                o.MigrationsAssembly(typeof(DocumentDbContext).Assembly.FullName);
            });

            o.UseSnakeCaseNamingConvention();

            o.ReplaceService<IModelCustomizer, DocumentModelBaseCustomizer>();
        });

        services.AddScoped<IDocumentDbContext, DocumentDbContext>();
    
        services.AddScoped<ITenantableRepository<Public.Document>, DocumentRepository>();
        services.AddScoped<IDocumentMetaRepository, DocumentRepository>();
        
        services.AddScoped<ITenantableRepository<Notification>, NotificationRepository>();
        services.AddScoped<INotificationMetaRepository, NotificationRepository>();

        services.AddScoped<ITenantableRepository<Subscription>, SubscriptionRepository>();
        services.AddScoped<ISubscriptionMetaRepository, SubscriptionRepository>();
        services.AddHostedService<InitializationWorker>();

        return services;
    }
}
