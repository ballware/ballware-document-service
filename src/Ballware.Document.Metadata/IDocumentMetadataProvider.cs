using Ballware.Document.Metadata;

namespace Ballware.Document.Metadata;

public interface IDocumentMetadataProvider
{
    byte[] DocumentBinaryForTenantAndId(Guid tenantId, Guid documentId);
    
    IEnumerable<DocumentSelectEntry> DocumentsForTenant(Guid tenantId);
    
    Guid AddDocumentMetadataForTenant(Guid tenantId, Guid userId, string? entity, string displayName, byte[] binary, string parameter);
    void UpdateDocumentMetadataForTenantAndId(Guid tenantId, Guid userId, Guid documentId, string? entity, string displayName, byte[] binary, string parameter);
}