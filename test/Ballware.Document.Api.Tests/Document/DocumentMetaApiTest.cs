using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Ballware.Document.Api.Endpoints;
using Ballware.Document.Api.Tests.Utils;
using Ballware.Shared.Authorization;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ballware.Document.Api.Tests.Document;

public class DocumentMetaApiTest : ApiMappingBaseTest
{
    [Test]
    public async Task HandleSelectListForEntity_succeeds()
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

        var principalUtilsMock = new Mock<IPrincipalUtils>();
        var tenantRightsCheckerMock = new Mock<ITenantRightsChecker>();
        var authorizationMetadataProviderMock = new Mock<IAuthorizationMetadataProvider>();
        var repositoryMock = new Mock<IDocumentMetaRepository>();
        
        var fakeTenantMock = new Mock<ITenantAuthorizationMetadata>();
        
        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedTenantId);

        tenantRightsCheckerMock
            .Setup(c => c.HasRightAsync(It.IsAny<ITenantAuthorizationMetadata>(), "meta", "document", It.IsAny<Dictionary<string, object>>(),
                "visiblestate.0"))
            .ReturnsAsync(false);
        
        tenantRightsCheckerMock
            .Setup(c => c.HasRightAsync(It.IsAny<ITenantAuthorizationMetadata>(), "meta", "document", It.IsAny<Dictionary<string, object>>(),
                "visiblestate.5"))
            .ReturnsAsync(true);
        
        authorizationMetadataProviderMock
            .Setup(r => r.MetadataForTenantByIdAsync(expectedTenantId))
            .ReturnsAsync(fakeTenantMock.Object);
        
        repositoryMock
            .Setup(r => r.SelectListForTenantAndEntityAsync(expectedTenantId, "entity1"))
            .ReturnsAsync(expectedList);

        var client = await CreateApplicationClientAsync("documentApi", services =>
        {
            services.AddSingleton<IPrincipalUtils>(principalUtilsMock.Object);
            services.AddSingleton<ITenantRightsChecker>(tenantRightsCheckerMock.Object);
            services.AddSingleton<IAuthorizationMetadataProvider>(authorizationMetadataProviderMock.Object);
            services.AddSingleton<IDocumentMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDocumentUserApi("document");
            });
        });
        
        // Act
        var response = await client.GetAsync($"document/selectlistdocumentsforentity/entity1");
        
        // Assert
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var result = JsonSerializer.Deserialize<IEnumerable<DocumentSelectListEntry>>(await response.Content.ReadAsStringAsync())?.ToList();
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(DeepComparer.AreListsEqual(expectedList.Where(x => x.State == 5), result, TestContext.WriteLine));
        });

        // Arrange
        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(Guid.NewGuid());
        
        // Act
        var notFoundResponse = await client.GetAsync($"document/selectlistdocumentsforentity/entity1");
        
        // Assert
        Assert.That(notFoundResponse.StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task HandleSelectList_succeeds()
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

        var principalUtilsMock = new Mock<IPrincipalUtils>();
        var tenantRightsCheckerMock = new Mock<ITenantRightsChecker>();
        var authorizationMetadataProviderMock = new Mock<IAuthorizationMetadataProvider>();
        var repositoryMock = new Mock<IDocumentMetaRepository>();

        var fakeTenantMock = new Mock<ITenantAuthorizationMetadata>();

        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedTenantId);

        tenantRightsCheckerMock
            .Setup(c => c.HasRightAsync(It.IsAny<ITenantAuthorizationMetadata>(), "meta", "document", It.IsAny<Dictionary<string, object>>(),
                "visiblestate.0"))
            .ReturnsAsync(false);
        
        tenantRightsCheckerMock
            .Setup(c => c.HasRightAsync(It.IsAny<ITenantAuthorizationMetadata>(), "meta", "document", It.IsAny<Dictionary<string, object>>(),
                "visiblestate.5"))
            .ReturnsAsync(true);
        
        authorizationMetadataProviderMock
            .Setup(r => r.MetadataForTenantByIdAsync(expectedTenantId))
            .ReturnsAsync(fakeTenantMock.Object);
        
        repositoryMock
            .Setup(r => r.SelectListForTenantAsync(expectedTenantId))
            .ReturnsAsync(expectedList);

        var client = await CreateApplicationClientAsync("documentApi", services =>
        {
            services.AddSingleton<IPrincipalUtils>(principalUtilsMock.Object);
            services.AddSingleton<ITenantRightsChecker>(tenantRightsCheckerMock.Object);
            services.AddSingleton<IAuthorizationMetadataProvider>(authorizationMetadataProviderMock.Object);
            services.AddSingleton<IDocumentMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDocumentUserApi("document");
            });
        });
        
        // Act
        var response = await client.GetAsync($"document/selectlist");
        
        // Assert
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var result = JsonSerializer.Deserialize<IEnumerable<DocumentSelectListEntry>>(await response.Content.ReadAsStringAsync())?.ToList();
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(DeepComparer.AreListsEqual(expectedList, result, TestContext.WriteLine));
        });

        // Arrange
        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(Guid.NewGuid());
        
        // Act
        var notFoundResponse = await client.GetAsync($"document/selectlistdocumentsforentity/entity1");
        
        // Assert
        Assert.That(notFoundResponse.StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task HandleSelectById_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedEntry = new DocumentSelectListEntry()
        {
            Id = Guid.NewGuid(),
            Name = "Name1",
            State = 0
        };

        var principalUtilsMock = new Mock<IPrincipalUtils>();
        var tenantRightsCheckerMock = new Mock<ITenantRightsChecker>();
        var authorizationMetadataProviderMock = new Mock<IAuthorizationMetadataProvider>();
        var repositoryMock = new Mock<IDocumentMetaRepository>();

        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedTenantId);
        
        repositoryMock
            .Setup(r => r.SelectByIdForTenantAsync(expectedTenantId, expectedEntry.Id))
            .ReturnsAsync(expectedEntry);

        var client = await CreateApplicationClientAsync("documentApi", services =>
        {
            services.AddSingleton<IPrincipalUtils>(principalUtilsMock.Object);
            services.AddSingleton<ITenantRightsChecker>(tenantRightsCheckerMock.Object);
            services.AddSingleton<IAuthorizationMetadataProvider>(authorizationMetadataProviderMock.Object);
            services.AddSingleton<IDocumentMetaRepository>(repositoryMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDocumentUserApi("document");
            });
        });
        
        // Act
        var response = await client.GetAsync($"document/selectbyid/{expectedEntry.Id}");
        
        // Assert
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var result = JsonSerializer.Deserialize<DocumentSelectListEntry>(await response.Content.ReadAsStringAsync());
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(DeepComparer.AreEqual(expectedEntry, result, TestContext.WriteLine));
        });

        // Arrange
        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(Guid.NewGuid());
        
        // Act
        var notFoundResponse = await client.GetAsync($"document/selectbyid/{expectedEntry.Id}");
        
        // Assert
        Assert.That(notFoundResponse.StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
    }
}