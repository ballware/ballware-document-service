using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ballware.Document.Api.Endpoints;
using Ballware.Document.Api.Tests.Utils;
using Ballware.Document.Data.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ballware.Document.Api.Tests.Subscription;

public class SubscriptionServiceApiTest : ApiMappingBaseTest
{
    [Test]
    public async Task HandleMetadataForTenantAndId_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedEntry = new Data.Public.Subscription()
        {
            Id = Guid.NewGuid(),
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Frequency = 1
        };
        
        var repositoryMock = new Mock<ISubscriptionMetaRepository>();

        repositoryMock
            .Setup(r => r.MetadataByTenantAndIdAsync(expectedTenantId, expectedEntry.Id))
            .ReturnsAsync(expectedEntry);

        // Act
        var client = await CreateApplicationClientAsync("serviceApi", services =>
        {
            services.AddSingleton<ISubscriptionMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscriptionServiceApi("subscription");
            });
        });
        
        var response = await client.GetAsync($"subscription/metadatabytenantandid/{expectedTenantId}/{expectedEntry.Id}");
        
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var result = JsonSerializer.Deserialize<Data.Public.Subscription>(await response.Content.ReadAsStringAsync());
        
        Assert.That(DeepComparer.AreEqual(expectedEntry, result, TestContext.WriteLine), Is.True);
        
        var notFoundResponse = await client.GetAsync($"subscription/metadatabytenantandid/{expectedTenantId}/{Guid.NewGuid()}");
        
        Assert.That(notFoundResponse.StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task HandleActiveSubscriptionsByFrequency_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        
        var expectedEntries = new List<Data.Public.Subscription>
        {
            new()
            {
                Id = Guid.NewGuid(),
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Frequency = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Frequency = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                NotificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Frequency = 1
            }
        };
        
        var repositoryMock = new Mock<ISubscriptionMetaRepository>();

        repositoryMock
            .Setup(r => r.GetActiveSubscriptionsByTenantAndFrequencyAsync(expectedTenantId, 1))
            .ReturnsAsync(expectedEntries);

        // Act
        var client = await CreateApplicationClientAsync("serviceApi", services =>
        {
            services.AddSingleton<ISubscriptionMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscriptionServiceApi("subscription");
            });
        });
        
        var response = await client.GetAsync($"subscription/activebytenantandfrequency/{expectedTenantId}/1");
        
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var result = JsonSerializer.Deserialize<IEnumerable<Data.Public.Subscription>>(await response.Content.ReadAsStringAsync());
        
        Assert.That(DeepComparer.AreListsEqual(expectedEntries, result, TestContext.WriteLine), Is.True);
    }
    
    [Test]
    public async Task HandleSetSendResult_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedSubscriptionId = Guid.NewGuid();
        var expectedError = "Test error message";
        
        var repositoryMock = new Mock<ISubscriptionMetaRepository>();
        
        repositoryMock
            .Setup(r => r.SetLastErrorAsync(expectedTenantId, expectedSubscriptionId, expectedError))
            .Callback((Guid tenantid, Guid subscriptionId, string error) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(tenantid, Is.EqualTo(expectedTenantId));
                    Assert.That(subscriptionId, Is.EqualTo(expectedSubscriptionId));
                    Assert.That(error, Is.EqualTo(expectedError));
                });
            });
        
        var client = await CreateApplicationClientAsync("serviceApi", services =>
        {
            services.AddSingleton<ISubscriptionMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscriptionServiceApi("subscription");
            });
        });

        // Act
        var response = await client.PostAsync($"subscription/setsendresult/{expectedTenantId}/{expectedSubscriptionId}",
            JsonContent.Create(expectedError));
        
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        repositoryMock.Verify(r => r.SetLastErrorAsync(
            expectedTenantId,
            expectedSubscriptionId,
            expectedError), Times.Once);
    }
}