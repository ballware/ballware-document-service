using Ballware.Document.Data.Ef.Configuration;
using Mapster;
using MapsterMapper;
using Ballware.Document.Data.Ef.SqlServer.Tests.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Data.Ef.SqlServer.Tests;

[TestFixture]
public class EfMigrationsTest : DatabaseBackedBaseTest
{
    [Test]
    public async Task Initialization_with_migrations_up_succeeds()
    {
        var storageOptions = PreparedBuilder.Configuration.GetSection("Storage").Get<StorageOptions>();
        var connectionString = MasterConnectionString;

        Assert.Multiple(() =>
        {
            Assert.That(storageOptions, Is.Not.Null);
            Assert.That(connectionString, Is.Not.Null);
        });

        PreparedBuilder.Services.AddBallwareDocumentStorageForSqlServer(storageOptions, connectionString);
        PreparedBuilder.Services.AddSingleton(new TypeAdapterConfig().AddBallwareDocumentStorageMappings());
        PreparedBuilder.Services.AddScoped<IMapper, ServiceMapper>();

        var app = PreparedBuilder.Build();

        await app.StartAsync();
        await app.StopAsync();
    }
}