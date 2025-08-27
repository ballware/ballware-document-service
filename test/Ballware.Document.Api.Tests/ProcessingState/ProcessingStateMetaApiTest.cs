using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using Ballware.Document.Api.Endpoints;
using Ballware.Document.Api.Tests.Utils;
using Ballware.Shared.Authorization;
using Ballware.Document.Data.Public;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Ballware.Document.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ballware.Document.Api.Tests.ProcessingState;

public class ProcessingStateMetaApiTest : ApiMappingBaseTest
{   
    [Test]
    public async Task HandleSelectListAllowedSuccessorsForDocumentByIdsAsync_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedDocumentId1 = Guid.NewGuid();
        var expectedDocumentId2 = Guid.NewGuid();

        var expectedEntityMock = new Mock<EntityMetadata>();
        
        var expectedList = new List<ProcessingStateSelectListEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                State = 1,
                Name = "Name 1",
                Finished = false,
                Locked = false,
                ReasonRequired = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                State = 2,
                Name = "Name 2",
                Finished = true,
                Locked = false,
                ReasonRequired = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                State = 3,
                Name = "Name 3",
                Finished = true,
                Locked = true,
                ReasonRequired = false
            }
        };
        
        var principalUtilsMock = new Mock<IPrincipalUtils>();
        var tenantRightsCheckerMock = new Mock<ITenantRightsChecker>();
        var entityRightsCheckerMock = new Mock<IEntityRightsChecker>();
        var authorizationMetadataProviderMock = new Mock<IAuthorizationMetadataProvider>();
        var documentMetaRepositoryMock = new Mock<IDocumentMetaRepository>();
        var notificationRepositoryMock = new Mock<INotificationMetaRepository>();
        var processingStateProviderMock = new Mock<IProcessingStateProvider>();

        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedTenantId);

        entityRightsCheckerMock
            .Setup(c => c.StateAllowedAsync(
                expectedTenantId,
                expectedEntityMock.Object,
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(false);
        
        entityRightsCheckerMock
            .Setup(c => c.StateAllowedAsync(
                expectedTenantId,
                expectedEntityMock.Object,
                expectedDocumentId1,
                It.IsIn(1, 3),
                It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(true);
        
        entityRightsCheckerMock
            .Setup(c => c.StateAllowedAsync(
                expectedTenantId,
                expectedEntityMock.Object,
                expectedDocumentId2,
                It.IsIn(2, 3),
                It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(true);
        
        processingStateProviderMock
            .Setup(r => r.SelectListPossibleSuccessorsForEntityAsync(expectedTenantId, "document", 10))
            .ReturnsAsync(expectedList);
        
        processingStateProviderMock
            .Setup(r => r.SelectListPossibleSuccessorsForEntityAsync(expectedTenantId, "document", 5))
            .ReturnsAsync(expectedList);

        authorizationMetadataProviderMock
            .Setup(r => r.MetadataForEntityByTenantAndIdentifierAsync(expectedTenantId, "document"))
            .ReturnsAsync(expectedEntityMock.Object);
        
        documentMetaRepositoryMock
            .Setup(r => r.GetCurrentStateForTenantAndIdAsync(expectedTenantId, expectedDocumentId1))
            .ReturnsAsync(10);
        
        documentMetaRepositoryMock
            .Setup(r => r.GetCurrentStateForTenantAndIdAsync(expectedTenantId, expectedDocumentId2))
            .ReturnsAsync(5);

        processingStateProviderMock
            .Setup(r => r.SelectListPossibleSuccessorsForEntityAsync(expectedTenantId, "document", 10))
            .ReturnsAsync(expectedList);
        
        processingStateProviderMock
            .Setup(r => r.SelectListPossibleSuccessorsForEntityAsync(expectedTenantId, "document", 5))
            .ReturnsAsync(expectedList);

        var client = await CreateApplicationClientAsync("documentApi", services =>
        {
            services.AddSingleton<IPrincipalUtils>(principalUtilsMock.Object);
            services.AddSingleton<ITenantRightsChecker>(tenantRightsCheckerMock.Object);
            services.AddSingleton<IEntityRightsChecker>(entityRightsCheckerMock.Object);
            services.AddSingleton<IAuthorizationMetadataProvider>(authorizationMetadataProviderMock.Object);
            services.AddSingleton<IDocumentMetaRepository>(documentMetaRepositoryMock.Object);
            services.AddSingleton<INotificationMetaRepository>(notificationRepositoryMock.Object);
            services.AddSingleton<IProcessingStateProvider>(processingStateProviderMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapProcessingStateDocumentApi("processingstate");
            });
        });
        
        // Act
        var responseNone = await client.GetAsync($"processingstate/selectlistallowedsuccessorsforentities/document");
        var response1 = await client.GetAsync($"processingstate/selectlistallowedsuccessorsforentities/document?id={expectedDocumentId1}");
        var response1And2 = await client.GetAsync($"processingstate/selectlistallowedsuccessorsforentities/document?id={expectedDocumentId1}&id={expectedDocumentId2}");
        
        // Assert
        Assert.That(responseNone.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response1.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response1And2.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var resultNone = JsonSerializer.Deserialize<IEnumerable<ProcessingStateSelectListEntry>>(await responseNone.Content.ReadAsStringAsync())?.ToList();
        var result1 = JsonSerializer.Deserialize<IEnumerable<ProcessingStateSelectListEntry>>(await response1.Content.ReadAsStringAsync())?.ToList();
        var result1And2 = JsonSerializer.Deserialize<IEnumerable<ProcessingStateSelectListEntry>>(await response1And2.Content.ReadAsStringAsync())?.ToList();
        
        Assert.Multiple(() =>
        {
            Assert.That(resultNone, Is.Not.Null);
            Assert.That(result1, Is.Not.Null);
            Assert.That(result1And2, Is.Not.Null);
            Assert.That(DeepComparer.AreListsEqual(expectedList.Where(s => s.State == 1 || s.State == 3), result1, TestContext.WriteLine));
            Assert.That(DeepComparer.AreListsEqual(expectedList.Where(s => s.State == 3), result1And2, TestContext.WriteLine));
        });
    }
    
    [Test]
    public async Task HandleSelectListAllowedSuccessorsForDocumentByIdsAsync_notFound()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        
        var principalUtilsMock = new Mock<IPrincipalUtils>();
        var tenantRightsCheckerMock = new Mock<ITenantRightsChecker>();
        var entityRightsCheckerMock = new Mock<IEntityRightsChecker>();
        var authorizationMetadataProviderMock = new Mock<IAuthorizationMetadataProvider>();
        var documentMetaRepositoryMock = new Mock<IDocumentMetaRepository>();
        var notificationRepositoryMock = new Mock<INotificationMetaRepository>();
        var processingStateProviderMock = new Mock<IProcessingStateProvider>();

        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedTenantId);
        
        authorizationMetadataProviderMock
            .Setup(r => r.MetadataForEntityByTenantAndIdentifierAsync(expectedTenantId, "document"))
            .ReturnsAsync(null as EntityMetadata);
        
        var client = await CreateApplicationClientAsync("documentApi", services =>
        {
            services.AddSingleton<IPrincipalUtils>(principalUtilsMock.Object);
            services.AddSingleton<ITenantRightsChecker>(tenantRightsCheckerMock.Object);
            services.AddSingleton<IEntityRightsChecker>(entityRightsCheckerMock.Object);
            services.AddSingleton<IAuthorizationMetadataProvider>(authorizationMetadataProviderMock.Object);
            services.AddSingleton<IDocumentMetaRepository>(documentMetaRepositoryMock.Object);
            services.AddSingleton<INotificationMetaRepository>(notificationRepositoryMock.Object);
            services.AddSingleton<IProcessingStateProvider>(processingStateProviderMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapProcessingStateDocumentApi("processingstate");
            });
        });
        
        // Act
        var responseNotFound = await client.GetAsync($"processingstate/selectlistallowedsuccessorsforentities/document");
        
        // Assert
        Assert.That(responseNotFound.StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task HandleSelectListAllowedSuccessorsForNotificationByIdsAsync_succeeds()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var expectedId1 = Guid.NewGuid();
        var expectedId2 = Guid.NewGuid();

        var expectedEntityMock = new Mock<EntityMetadata>();
        
        var expectedList = new List<ProcessingStateSelectListEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                State = 1,
                Name = "Name 1",
                Finished = false,
                Locked = false,
                ReasonRequired = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                State = 2,
                Name = "Name 2",
                Finished = true,
                Locked = false,
                ReasonRequired = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                State = 3,
                Name = "Name 3",
                Finished = true,
                Locked = true,
                ReasonRequired = false
            }
        };
        
        var principalUtilsMock = new Mock<IPrincipalUtils>();
        var tenantRightsCheckerMock = new Mock<ITenantRightsChecker>();
        var entityRightsCheckerMock = new Mock<IEntityRightsChecker>();
        var authorizationMetadataProviderMock = new Mock<IAuthorizationMetadataProvider>();
        var documentMetaRepositoryMock = new Mock<IDocumentMetaRepository>();
        var notificationMetaRepositoryMock = new Mock<INotificationMetaRepository>();
        var processingStateProviderMock = new Mock<IProcessingStateProvider>();

        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedTenantId);

        entityRightsCheckerMock
            .Setup(c => c.StateAllowedAsync(
                expectedTenantId,
                expectedEntityMock.Object,
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(false);
        
        entityRightsCheckerMock
            .Setup(c => c.StateAllowedAsync(
                expectedTenantId,
                expectedEntityMock.Object,
                expectedId1,
                It.IsIn(1, 3),
                It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(true);
        
        entityRightsCheckerMock
            .Setup(c => c.StateAllowedAsync(
                expectedTenantId,
                expectedEntityMock.Object,
                expectedId2,
                It.IsIn(2, 3),
                It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(true);
        
        processingStateProviderMock
            .Setup(r => r.SelectListPossibleSuccessorsForEntityAsync(expectedTenantId, "notification", 10))
            .ReturnsAsync(expectedList);
        
        processingStateProviderMock
            .Setup(r => r.SelectListPossibleSuccessorsForEntityAsync(expectedTenantId, "notification", 5))
            .ReturnsAsync(expectedList);

        authorizationMetadataProviderMock
            .Setup(r => r.MetadataForEntityByTenantAndIdentifierAsync(expectedTenantId, "notification"))
            .ReturnsAsync(expectedEntityMock.Object);
        
        notificationMetaRepositoryMock
            .Setup(r => r.GetCurrentStateForTenantAndIdAsync(expectedTenantId, expectedId1))
            .ReturnsAsync(10);
        
        notificationMetaRepositoryMock
            .Setup(r => r.GetCurrentStateForTenantAndIdAsync(expectedTenantId, expectedId2))
            .ReturnsAsync(5);

        processingStateProviderMock
            .Setup(r => r.SelectListPossibleSuccessorsForEntityAsync(expectedTenantId, "notification", 10))
            .ReturnsAsync(expectedList);
        
        processingStateProviderMock
            .Setup(r => r.SelectListPossibleSuccessorsForEntityAsync(expectedTenantId, "notification", 5))
            .ReturnsAsync(expectedList);

        var client = await CreateApplicationClientAsync("documentApi", services =>
        {
            services.AddSingleton<IPrincipalUtils>(principalUtilsMock.Object);
            services.AddSingleton<ITenantRightsChecker>(tenantRightsCheckerMock.Object);
            services.AddSingleton<IEntityRightsChecker>(entityRightsCheckerMock.Object);
            services.AddSingleton<IAuthorizationMetadataProvider>(authorizationMetadataProviderMock.Object);
            services.AddSingleton<IDocumentMetaRepository>(documentMetaRepositoryMock.Object);
            services.AddSingleton<INotificationMetaRepository>(notificationMetaRepositoryMock.Object);
            services.AddSingleton<IProcessingStateProvider>(processingStateProviderMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapProcessingStateDocumentApi("processingstate");
            });
        });
        
        // Act
        var responseNone = await client.GetAsync($"processingstate/selectlistallowedsuccessorsforentities/notification");
        var response1 = await client.GetAsync($"processingstate/selectlistallowedsuccessorsforentities/notification?id={expectedId1}");
        var response1And2 = await client.GetAsync($"processingstate/selectlistallowedsuccessorsforentities/notification?id={expectedId1}&id={expectedId2}");
        
        // Assert
        Assert.That(responseNone.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response1.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response1And2.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        
        var resultNone = JsonSerializer.Deserialize<IEnumerable<ProcessingStateSelectListEntry>>(await responseNone.Content.ReadAsStringAsync())?.ToList();
        var result1 = JsonSerializer.Deserialize<IEnumerable<ProcessingStateSelectListEntry>>(await response1.Content.ReadAsStringAsync())?.ToList();
        var result1And2 = JsonSerializer.Deserialize<IEnumerable<ProcessingStateSelectListEntry>>(await response1And2.Content.ReadAsStringAsync())?.ToList();
        
        Assert.Multiple(() =>
        {
            Assert.That(resultNone, Is.Not.Null);
            Assert.That(result1, Is.Not.Null);
            Assert.That(result1And2, Is.Not.Null);
            Assert.That(DeepComparer.AreListsEqual(expectedList.Where(s => s.State == 1 || s.State == 3), result1, TestContext.WriteLine));
            Assert.That(DeepComparer.AreListsEqual(expectedList.Where(s => s.State == 3), result1And2, TestContext.WriteLine));
        });
    }
    
    [Test]
    public async Task HandleSelectListAllowedSuccessorsForNotificationByIdsAsync_notFound()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        
        var principalUtilsMock = new Mock<IPrincipalUtils>();
        var tenantRightsCheckerMock = new Mock<ITenantRightsChecker>();
        var entityRightsCheckerMock = new Mock<IEntityRightsChecker>();
        var authorizationMetadataProviderMock = new Mock<IAuthorizationMetadataProvider>();
        var documentMetaRepositoryMock = new Mock<IDocumentMetaRepository>();
        var notificationMetaRepositoryMock = new Mock<INotificationMetaRepository>();
        var processingStateProviderMock = new Mock<IProcessingStateProvider>();

        principalUtilsMock
            .Setup(p => p.GetUserTenandId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedTenantId);
        
        authorizationMetadataProviderMock
            .Setup(r => r.MetadataForEntityByTenantAndIdentifierAsync(expectedTenantId, "notification"))
            .ReturnsAsync(null as EntityMetadata);
        
        var client = await CreateApplicationClientAsync("documentApi", services =>
        {
            services.AddSingleton<IPrincipalUtils>(principalUtilsMock.Object);
            services.AddSingleton<ITenantRightsChecker>(tenantRightsCheckerMock.Object);
            services.AddSingleton<IEntityRightsChecker>(entityRightsCheckerMock.Object);
            services.AddSingleton<IAuthorizationMetadataProvider>(authorizationMetadataProviderMock.Object);
            services.AddSingleton<IDocumentMetaRepository>(documentMetaRepositoryMock.Object);
            services.AddSingleton<INotificationMetaRepository>(notificationMetaRepositoryMock.Object);
            services.AddSingleton<IProcessingStateProvider>(processingStateProviderMock.Object);
        }, app =>
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapProcessingStateDocumentApi("processingstate");
            });
        });
        
        // Act
        var responseNotFound = await client.GetAsync($"processingstate/selectlistallowedsuccessorsforentities/notification");
        
        // Assert
        Assert.That(responseNotFound.StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
    }
}