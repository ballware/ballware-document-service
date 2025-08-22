using Ballware.Document.Data.SelectLists;

namespace Ballware.Document.Metadata;

public interface IDocumentMetadataProvider
{
    Task<byte[]> DocumentBinaryForTenantAndIdAsync(Guid tenantId, Guid documentId);
    
    Task<IEnumerable<DocumentSelectListEntry>> DocumentsForTenantAsync(Guid tenantId);
    
    Task<Guid> AddDocumentMetadataForTenantAsync(Guid tenantId, Guid userId, string? entity, string displayName, byte[] binary, string parameter);
    Task UpdateDocumentMetadataAndBinaryForTenantAndIdAsync(Guid tenantId, Guid userId, Guid documentId, string? entity, string displayName, byte[] binary, string parameter);
    Task UpdateDocumentBinaryForTenantAndIdAsync(Guid tenantId, Guid userId, Guid documentId, byte[] binary);
}