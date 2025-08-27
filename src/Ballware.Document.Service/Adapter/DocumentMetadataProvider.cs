using System.Collections.Immutable;
using Ballware.Document.Data.Repository;
using Ballware.Document.Metadata;
using Ballware.Storage.Service.Client;
using DocumentSelectListEntry = Ballware.Document.Data.SelectLists.DocumentSelectListEntry;

namespace Ballware.Document.Service.Adapter;

public class DocumentMetadataProvider : IDocumentMetadataProvider
{ 
    private string StorageDocumentEntity { get; } = "document";
    private string StorageDocumentBinaryFilename { get; } = "report.xml";
    
    private IDocumentMetaRepository Repository { get; }
    private StorageServiceClient StorageClient { get; }
    
    public DocumentMetadataProvider(IDocumentMetaRepository repository, StorageServiceClient storageClient)
    {
        Repository = repository;
        StorageClient = storageClient;   
    }
    
    public async Task<byte[]> DocumentBinaryForTenantAndIdAsync(Guid tenantId, Guid documentId)
    {
        var document = await Repository.MetadataByTenantAndIdAsync(tenantId, documentId);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {documentId} not found for tenant {tenantId}.");
        }
        
        var documentBinary = await StorageClient.AttachmentDownloadForTenantEntityAndOwnerByFilenameAsync(tenantId, StorageDocumentEntity, documentId, StorageDocumentBinaryFilename);

        if (documentBinary == null)
        {
            throw new InvalidOperationException($"Report binary attachment for ID {documentId} not found for tenant {tenantId}.");
        }

        using var stream = new MemoryStream();
        
        await documentBinary.Stream.CopyToAsync(stream);
        
        return stream.ToArray();
    }

    public async Task<IEnumerable<DocumentSelectListEntry>> DocumentsForTenantAsync(Guid tenantId)
    {
        return await Repository.SelectListForTenantAsync(tenantId);
    }

    public async Task<Guid> AddDocumentMetadataForTenantAsync(Guid tenantId, Guid userId, string? entity, string displayName, byte[] binary,
        string parameter)
    {
        var document = await Repository.NewAsync(tenantId, "primary", ImmutableDictionary<string, object>.Empty);
        
        document.Entity = entity;
        document.DisplayName = displayName;
        document.ReportParameter = parameter;
        
        await Repository.SaveAsync(tenantId, userId, "primary", ImmutableDictionary<string, object>.Empty, document);
        
        using var stream = new MemoryStream(binary);
        
        await StorageClient.AttachmentUploadForTenantEntityAndOwnerBehalfOfUserAsync(tenantId, userId, StorageDocumentEntity, document.Id,
            [new FileParameter(stream, StorageDocumentBinaryFilename, "application/xml")]);
        
        return document.Id;
    }

    public async Task UpdateDocumentMetadataAndBinaryForTenantAndIdAsync(Guid tenantId, Guid userId, Guid documentId, string? entity,
        string displayName, byte[] binary, string parameter)
    {
        var document = await Repository.MetadataByTenantAndIdAsync(tenantId, documentId);
        
        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {documentId} not found for tenant {tenantId}.");
        }
        
        document.Entity = entity;
        document.DisplayName = displayName;
        document.ReportParameter = parameter;
        
        await Repository.SaveAsync(tenantId, userId, "primary", ImmutableDictionary<string, object>.Empty, document);
        
        using var stream = new MemoryStream(binary);
        
        await StorageClient.AttachmentUploadForTenantEntityAndOwnerBehalfOfUserAsync(tenantId, userId, StorageDocumentEntity, document.Id,
            [new FileParameter(stream, StorageDocumentBinaryFilename, "application/xml")]);
    }
    
    public async Task UpdateDocumentBinaryForTenantAndIdAsync(Guid tenantId, Guid userId, Guid documentId, byte[] binary)
    {
        var document = await Repository.MetadataByTenantAndIdAsync(tenantId, documentId);
        
        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {documentId} not found for tenant {tenantId}.");
        }
        
        using var stream = new MemoryStream(binary);
        
        await StorageClient.AttachmentUploadForTenantEntityAndOwnerBehalfOfUserAsync(tenantId, userId, StorageDocumentEntity, document.Id,
            [new FileParameter(stream, StorageDocumentBinaryFilename, "application/xml")]);
    }
}