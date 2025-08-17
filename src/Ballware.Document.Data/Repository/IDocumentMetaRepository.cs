using Ballware.Document.Data.SelectLists;
using Ballware.Shared.Data.Repository;

namespace Ballware.Document.Data.Repository;

public interface IDocumentMetaRepository : ITenantableRepository<Public.Document>
{
    Task<Public.Document?> MetadataByTenantAndIdAsync(Guid tenantId, Guid id);

    Task<IEnumerable<DocumentSelectListEntry>> SelectListForTenantAsync(Guid tenantId);

    Task<IEnumerable<DocumentSelectListEntry>> SelectListForTenantAndEntityAsync(Guid tenantId, string entity);
    
    Task<DocumentSelectListEntry?> SelectByIdForTenantAsync(Guid tenantId, Guid id);
    
    Task<int?> GetCurrentStateForTenantAndIdAsync(Guid tenantId, Guid id);
    
    Task<string> GenerateListQueryAsync(Guid tenantId);
}