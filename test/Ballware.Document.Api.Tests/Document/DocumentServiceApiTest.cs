using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Ballware.Document.Api.Endpoints;
using Ballware.Document.Api.Tests.Utils;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ballware.Document.Api.Tests.Document;

public class DocumentServiceApiTest : ApiMappingBaseTest
{
    [Test]
    public async Task HandleMetadataForTenantAndId_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedEntry = new Data.Public.Document()
        {
            Id = Guid.NewGuid(),
            DisplayName = "My Document",
            Entity = "entity1"
        };

        var repositoryMock = new Mock<IDocumentMetaRepository>();

        repositoryMock
            .Setup(r => r.MetadataByTenantAndIdAsync(expectedTenantId, expectedEntry.Id))
            .ReturnsAsync(expectedEntry);

        // Act
        var client = await CreateApplicationClientAsync("serviceApi", services =>
        {
            services.AddSingleton<IDocumentMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDocumentServiceApi("document");
            });
        });
        
        var response = await client.GetAsync($"document/documentmetadatabytenantandid/{expectedTenantId}/{expectedEntry.Id}");
        
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var result = JsonSerializer.Deserialize<Data.Public.Document>(await response.Content.ReadAsStringAsync());
        
        Assert.That(DeepComparer.AreEqual(expectedEntry, result, TestContext.WriteLine));
        
        var notFoundResponse = await client.GetAsync($"document/documentmetadatabytenantandid/{expectedTenantId}/{Guid.NewGuid()}");
        
        Assert.That(notFoundResponse.StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task HandleNewForTenant_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedEntry = new Data.Public.Document()
        {
            Id = Guid.NewGuid(),
            DisplayName = "My Document",
            Entity = "entity1"
        };

        var repositoryMock = new Mock<IDocumentMetaRepository>();

        repositoryMock
            .Setup(r => r.NewAsync(expectedTenantId, "primary", It.IsAny<IDictionary<string, object>>()))
            .ReturnsAsync(expectedEntry);

        // Act
        var client = await CreateApplicationClientAsync("serviceApi", services =>
        {
            services.AddSingleton<IDocumentMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDocumentServiceApi("document");
            });
        });
        
        var response = await client.GetAsync($"document/documenttemplatebehalfofuserbytenant/{expectedTenantId}");
        
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var result = JsonSerializer.Deserialize<Data.Public.Document>(await response.Content.ReadAsStringAsync());
        
        Assert.That(result, Is.Not.Null);
    }
    
    [Test]
    public async Task HandleSaveForTenantBehalfOfUser_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedUserId = Guid.NewGuid();
        var expectedEntry = new Data.Public.Document()
        {
            Id = Guid.NewGuid(),
            DisplayName = "My Document",
            Entity = "entity1"
        };

        var repositoryMock = new Mock<IDocumentMetaRepository>();

        repositoryMock
            .Setup(r => r.SaveAsync(expectedTenantId, expectedUserId, "primary",
                It.IsAny<IDictionary<string, object>>(), It.IsAny<Data.Public.Document>()))
            .Callback<Guid, Guid?, string, IDictionary<string, object>, Data.Public.Document>((tenantId, userId, identifier, claims, content) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(tenantId, Is.EqualTo(expectedTenantId));
                    Assert.That(userId, Is.EqualTo(expectedUserId));
                    Assert.That(identifier, Is.EqualTo("primary"));
                    Assert.That(DeepComparer.AreEqual(content, expectedEntry, TestContext.WriteLine));
                });
                
            });

        // Act
        var client = await CreateApplicationClientAsync("serviceApi", services =>
        {
            services.AddSingleton<IDocumentMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDocumentServiceApi("document");
            });
        });
        
        var response = await client.PostAsync($"document/savedocumentbehalfofuser/{expectedTenantId}/{expectedUserId}", 
            JsonContent.Create(expectedEntry));
        
        // Assert
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));

        repositoryMock.Verify(r => r.SaveAsync(
            expectedTenantId,
            expectedUserId,
            "primary",
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<Data.Public.Document>()), Times.Once);
    }
    
    [Test]
    public async Task HandleSelectListForTenant_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedList = new List<DocumentSelectListEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Name1",
                State = 0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Name2",
                State = 5
            }
        };

        var repositoryMock = new Mock<IDocumentMetaRepository>();

        repositoryMock
            .Setup(r => r.SelectListForTenantAsync(expectedTenantId))
            .ReturnsAsync(expectedList);

        var client = await CreateApplicationClientAsync("serviceApi", services =>
        {
            services.AddSingleton<IDocumentMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDocumentServiceApi("document");
            });
        });
        
        // Act
        var response = await client.GetAsync($"document/selectlistdocumentsfortenant/{expectedTenantId}");
        
        // Assert
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var result = JsonSerializer.Deserialize<IEnumerable<DocumentSelectListEntry>>(await response.Content.ReadAsStringAsync())?.ToList();
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(DeepComparer.AreListsEqual(expectedList, result, TestContext.WriteLine));
        });
    }
}