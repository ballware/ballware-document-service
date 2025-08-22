using Ballware.Shared.Api;
using Ballware.Storage.Service.Client;

namespace Ballware.Document.Service.Adapter;

public class StorageServiceProvider : IFileStorageProvider
{
    private StorageServiceClient StorageClient { get; }
    
    public StorageServiceProvider(StorageServiceClient storageClient)
    {
        StorageClient = storageClient;
    }
    
    public async Task<Stream> TemporaryFileByIdAsync(Guid tenantId, Guid temporaryId)
    {
        return (await StorageClient.TemporaryDownloadForTenantByIdAsync(tenantId, temporaryId)).Stream;
    }

    public async Task UploadTemporaryFileBehalfOfUserAsync(Guid tenantId, Guid userId, Guid temporaryId, string fileName,
        string contentType, Stream data)
    {
        await StorageClient.TemporaryUploadForTenantAndIdBehalfOfUserAsync(tenantId, userId, temporaryId, [
            new FileParameter(data, fileName, contentType)
        ]);
    }

    public async Task RemoveTemporaryFileByIdBehalfOfUserAsync(Guid tenantId, Guid userId, Guid temporaryId)
    {
        await StorageClient.TemporaryDropForTenantAndIdBehalfOfUserAsync(tenantId, userId, temporaryId);
    }
}