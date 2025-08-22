using Ballware.Shared.Data.Repository;
using Ballware.Storage.Service.Client;

namespace Ballware.Document.Service.Extensions;

public class DocumentImportExportRepositoryHook : ITenantableRepositoryHook<Ballware.Document.Data.Public.Document, Ballware.Document.Data.Persistables.Document>
{
    private StorageServiceClient StorageClient { get; }
    
    public DocumentImportExportRepositoryHook(StorageServiceClient storageClient)
    {
        StorageClient = storageClient;   
    }
    
    public Data.Public.Document ExtendById(Guid tenantId, string identifier, IDictionary<string, object> claims, Data.Public.Document value)
    {
        if ("exportjson".Equals(identifier, StringComparison.InvariantCultureIgnoreCase))
        {
            var reportBinaryResponse = StorageClient.AttachmentDownloadForTenantEntityAndOwnerByFilename(tenantId, "document", value.Id, "report.xml");

            if (reportBinaryResponse != null)
            {
                using var stream = new MemoryStream();
                
                reportBinaryResponse.Stream.CopyTo(stream);
                
                value.ReportBinary = stream.ToArray();
            }
        }

        return value;
    }

    public void BeforeSave(Guid tenantId, Guid? userId, string identifier, IDictionary<string, object> claims, Data.Public.Document value, bool insert)
    {
        if ("importjson".Equals(identifier, StringComparison.InvariantCultureIgnoreCase))
        {
            if (value.ReportBinary != null && userId != null)
            {
                using var stream = new MemoryStream(value.ReportBinary);
                
                StorageClient.AttachmentUploadForTenantEntityAndOwnerBehalfOfUser(tenantId, userId.Value, "document", value.Id,
                    [new FileParameter(stream, "report.xml", "application/xml")]);
            }
        }
    }

    public void BeforeRemove(Guid tenantId, Guid? userId, IDictionary<string, object> claims, Data.Persistables.Document persistable)
    {
        StorageClient.AttachmentDropForTenantEntityAndOwnerByFilenameBehalfOfUser(tenantId, userId.Value, "document", persistable.Uuid, "report.xml");
    }
}