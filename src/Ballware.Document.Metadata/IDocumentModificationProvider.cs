namespace Ballware.Document.Metadata;

public interface IDocumentModificationProvider
{
    Task UpdateDatasourcesAsync(
        Guid tenantId,
        Guid userId,
        Guid documentId
        );
}