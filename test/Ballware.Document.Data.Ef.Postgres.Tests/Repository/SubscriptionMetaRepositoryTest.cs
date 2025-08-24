using System.Collections.Immutable;
using System.Text;
using Ballware.Document.Data.Public;
using Ballware.Document.Data.Repository;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Ballware.Document.Data.Ef.Postgres.Tests.Repository;

public class SubscriptionMetaRepositoryTest : RepositoryBaseTest
{
    [Test]
    public async Task Save_and_remove_value_succeeds()
    {
        using var scope = Application.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<ISubscriptionMetaRepository>();

        var expectedValue = await repository.NewQueryAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, ImmutableDictionary<string, object>.Empty);
        
        expectedValue.NotificationId = Guid.NewGuid();
        expectedValue.UserId = Guid.NewGuid();
        expectedValue.Active = true;
        expectedValue.Attachment = true;
        expectedValue.AttachmentFileName = "attachment.txt";
        expectedValue.Body = "fake body";
        expectedValue.Frequency = 1;
        expectedValue.Mail = "fake@mail.com";

        await repository.SaveAsync(TenantId, null, "primary", ImmutableDictionary<string, object>.Empty, expectedValue);

        var actualValue = await repository.ByIdAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, expectedValue.Id);
        var actualById = await repository.MetadataByTenantAndIdAsync(TenantId, expectedValue.Id);
        
        Assert.Multiple(() =>
        {
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue?.Id, Is.EqualTo(expectedValue.Id));
            Assert.That(actualValue?.NotificationId, Is.EqualTo(expectedValue.NotificationId));
            Assert.That(actualValue?.UserId, Is.EqualTo(expectedValue.UserId));
            Assert.That(actualValue?.Active, Is.EqualTo(expectedValue.Active));
            Assert.That(actualValue?.Attachment, Is.EqualTo(expectedValue.Attachment));
            Assert.That(actualValue?.Body, Is.EqualTo(expectedValue.Body));
            Assert.That(actualValue?.Frequency, Is.EqualTo(expectedValue.Frequency));
            Assert.That(actualValue?.Mail, Is.EqualTo(expectedValue.Mail));
            
            Assert.That(actualById, Is.Not.Null);
            Assert.That(actualById?.Id, Is.EqualTo(expectedValue.Id));
            Assert.That(actualById?.NotificationId, Is.EqualTo(expectedValue.NotificationId));
            Assert.That(actualById?.UserId, Is.EqualTo(expectedValue.UserId));
            Assert.That(actualById?.Active, Is.EqualTo(expectedValue.Active));
            Assert.That(actualById?.Attachment, Is.EqualTo(expectedValue.Attachment));
            Assert.That(actualById?.Body, Is.EqualTo(expectedValue.Body));
            Assert.That(actualById?.Frequency, Is.EqualTo(expectedValue.Frequency));
            Assert.That(actualById?.Mail, Is.EqualTo(expectedValue.Mail));
        });

        await repository.SetLastErrorAsync(TenantId, actualValue.Id, "fake error message");
        
        actualValue = await repository.ByIdAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, expectedValue.Id);

        Assert.Multiple(() =>
        {
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue?.Id, Is.EqualTo(expectedValue.Id));
            Assert.That(actualValue?.LastError, Is.EqualTo("fake error message"));
            Assert.That(actualValue?.LastSendStamp, Is.Not.Null);
        });
        
        var removeParams = new Dictionary<string, object>([new KeyValuePair<string, object>("Id", expectedValue.Id)]);

        var removeResult = await repository.RemoveAsync(TenantId, null, ImmutableDictionary<string, object>.Empty, removeParams);

        Assert.Multiple(() =>
        {
            Assert.That(removeResult.Result, Is.True);
        });

        actualValue = await repository.ByIdAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, expectedValue.Id);

        Assert.That(actualValue, Is.Null);
    }

    [Test]
    public async Task Query_tenant_items_succeeds()
    {
        using var scope = Application.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<ISubscriptionMetaRepository>();

        var fakeTenantIds = new[] { Guid.NewGuid(), Guid.NewGuid(), TenantId, Guid.NewGuid() };

        List<Guid> fakeValueIds = new List<Guid>();
        
        foreach (var fakeTenant in fakeTenantIds)
        {
            for (var i = 0; i < 10; i++)
            {
                var fakeValue = await repository.NewAsync(fakeTenant, "primary", ImmutableDictionary<string, object>.Empty);

                fakeValue.NotificationId = Guid.NewGuid();
                fakeValue.UserId = Guid.NewGuid();
                fakeValue.Active = true;
                fakeValue.Attachment = true;
                fakeValue.AttachmentFileName = "attachment.txt";
                fakeValue.Body = "fake body";
                fakeValue.Frequency = 1;
                fakeValue.Mail = "fake@mail.com";
                
                await repository.SaveAsync(fakeTenant, null, "primary", ImmutableDictionary<string, object>.Empty, fakeValue);

                if (fakeTenant == TenantId)
                {
                    fakeValueIds.Add(fakeValue.Id);    
                }
            }
        }

        var actualTenantItemsCount = await repository.CountAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, ImmutableDictionary<string, object>.Empty);
        var actualTenantAllItems = await repository.AllAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty);
        var actualTenantQueryItems = await repository.QueryAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, ImmutableDictionary<string, object>.Empty);
        var actualTenantFrequencyItems = await repository.GetActiveSubscriptionsByTenantAndFrequencyAsync(TenantId, 1);
        
        var actualSelectListItems = await repository.SelectListForTenantAsync(TenantId);
        var actualSelectByIdItem = await repository.SelectByIdForTenantAsync(TenantId, fakeValueIds[0]);
        
        Assert.Multiple(() =>
        {
            Assert.That(actualTenantItemsCount, Is.EqualTo(10));
            Assert.That(actualTenantAllItems.Count(), Is.EqualTo(10));
            Assert.That(actualTenantQueryItems.Count(), Is.EqualTo(10));
            Assert.That(actualTenantFrequencyItems.Count(), Is.EqualTo(10));
            Assert.That(actualSelectListItems.Count(), Is.EqualTo(10));
            Assert.That(actualSelectByIdItem, Is.Not.Null);
            Assert.That(actualSelectByIdItem?.Id, Is.EqualTo(fakeValueIds[0]));
        });
    }

    [Test]
    public async Task Import_values_succeeds()
    {
        using var scope = Application.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<ISubscriptionMetaRepository>();

        var importList = new List<Subscription>();

        for (var i = 0; i < 10; i++)
        {
            var fakeValue = await repository.NewAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty);

            fakeValue.NotificationId = Guid.NewGuid();
            fakeValue.UserId = Guid.NewGuid();
            fakeValue.Active = true;
            fakeValue.Attachment = true;
            fakeValue.AttachmentFileName = "attachment.txt";
            fakeValue.Body = "fake body";
            fakeValue.Frequency = 1;
            fakeValue.Mail = "fake@mail.com";
            
            importList.Add(fakeValue);
        }

        var importBinary = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(importList));

        using var importStream = new MemoryStream(importBinary);

        await repository.ImportAsync(TenantId, null, "primary", ImmutableDictionary<string, object>.Empty, importStream, (doc) => Task.FromResult(true));

        var actualTenantItemsCount = await repository.CountAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, ImmutableDictionary<string, object>.Empty);
        var actualTenantAllItems = await repository.AllAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty);
        var actualTenantQueryItems = await repository.QueryAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, ImmutableDictionary<string, object>.Empty);
        
        Assert.Multiple(() =>
        {
            Assert.That(actualTenantItemsCount, Is.EqualTo(10));
            Assert.That(actualTenantAllItems.Count(), Is.EqualTo(10));
            Assert.That(actualTenantQueryItems.Count(), Is.EqualTo(10));
        });
    }

    [Test]
    public async Task Export_values_succeeds()
    {
        using var scope = Application.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<ISubscriptionMetaRepository>();

        var exportIdList = new List<Guid>();
        var exportItemList = new List<Subscription>();

        for (var i = 0; i < 10; i++)
        {
            var fakeValue = await repository.NewAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty);

            fakeValue.NotificationId = Guid.NewGuid();
            fakeValue.UserId = Guid.NewGuid();
            fakeValue.Active = true;
            fakeValue.Attachment = true;
            fakeValue.AttachmentFileName = "attachment.txt";
            fakeValue.Body = "fake body";
            fakeValue.Frequency = 1;
            fakeValue.Mail = "fake@mail.com";
            
            await repository.SaveAsync(TenantId, null, "primary", ImmutableDictionary<string, object>.Empty, fakeValue);

            if (i % 2 == 0)
            {
                exportIdList.Add(fakeValue.Id);
                exportItemList.Add(fakeValue);
            }
        }

        var idStringValues = new StringValues(exportIdList.Select(id => id.ToString()).ToArray());
        
        var exportResult = await repository.ExportAsync(TenantId, "primary", ImmutableDictionary<string, object>.Empty, new Dictionary<string, object>(new[] { new KeyValuePair<string, object>("id", idStringValues) }));

        Assert.Multiple(() =>
        {
            Assert.That(exportResult.FileName, Is.EqualTo("primary.json"));
            Assert.That(exportResult.MediaType, Is.EqualTo("application/json"));
            Assert.That(exportResult.Data, Is.Not.Null);

            using var inputStream = new MemoryStream(exportResult.Data);
            using var streamReader = new StreamReader(inputStream);

            var actualItems = JsonConvert.DeserializeObject<IEnumerable<Subscription>>(streamReader.ReadToEnd())?.ToList();

            Assert.That(actualItems, Is.Not.Null);
            Assert.That(actualItems?.Count, Is.EqualTo(5));
            Assert.That(actualItems?.Select(item => item.Id), Is.EquivalentTo(exportItemList.Select(item => item.Id)));
        });
    }
    
    [Test]
    public async Task Execute_generated_list_query_succeeds()
    {
        using var scope = Application.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<ISubscriptionMetaRepository>();

        var listQuery = await repository.GenerateListQueryAsync(TenantId);

        var connection = dbContext.Database.GetDbConnection();
        
        var result = await connection.QueryAsync(listQuery);
        
        Assert.Multiple(() =>
        {
            Assert.That(result.Count(), Is.EqualTo(0));
        });
    }
}