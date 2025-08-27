using AutoMapper;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Ballware.Shared.Data.Ef.Repository;
using Ballware.Shared.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Ballware.Document.Data.Ef.Repository;

public abstract class DocumentBaseRepository : TenantableBaseRepository<Public.Document, Persistables.Document>, IDocumentMetaRepository
{
    private IDocumentDbContext DocumentDbContext { get; }

    public DocumentBaseRepository(IMapper mapper, IDocumentDbContext dbContext,
        ITenantableRepositoryHook<Public.Document, Persistables.Document>? hook = null)
        : base(mapper, dbContext, hook)
    {
        DocumentDbContext = dbContext;
    }

    public virtual async Task<Public.Document?> MetadataByTenantAndIdAsync(Guid tenantId, Guid id)
    {
        var result = await DocumentDbContext.Documents.SingleOrDefaultAsync(d => d.TenantId == tenantId && d.Uuid == id);

        return result != null ? Mapper.Map<Public.Document>(result) : null;
    }

    public virtual async Task<IEnumerable<DocumentSelectListEntry>> SelectListForTenantAsync(Guid tenantId)
    {
        return await Task.FromResult(DocumentDbContext.Documents
            .Where(p => p.TenantId == tenantId)
            .Select(d => new { d.Uuid, d.DisplayName, d.State, d.ReportParameter })
            .OrderBy(c => c.DisplayName)
            .Select(d => new DocumentSelectListEntry { Id = d.Uuid, Name = d.DisplayName, State = d.State, ReportParameter = d.ReportParameter}));
    }
    
    public virtual async Task<DocumentSelectListEntry?> SelectByIdForTenantAsync(Guid tenantId, Guid id)
    {
        return await DocumentDbContext.Documents.Where(r => r.TenantId == tenantId && r.Uuid == id)
            .Select(d => new DocumentSelectListEntry { Id = d.Uuid, Name = d.DisplayName, State = d.State, ReportParameter = d.ReportParameter })
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetCurrentStateForTenantAndIdAsync(Guid tenantId, Guid id)
    {
        return await DocumentDbContext.Documents
            .Where(d => d.TenantId == tenantId && d.Uuid == id)
            .Select(d => (int?)d.State)
            .FirstOrDefaultAsync();
    }

    public virtual async Task<IEnumerable<DocumentSelectListEntry>> SelectListForTenantAndEntityAsync(Guid tenantId, string entity)
    {
        return await Task.FromResult(DocumentDbContext.Documents
            .Where(p => p.TenantId == tenantId && p.Entity == entity)
            .Select(d => new { d.Uuid, d.DisplayName, d.State })
            .OrderBy(c => c.DisplayName)
            .Select(d => new DocumentSelectListEntry { Id = d.Uuid, Name = d.DisplayName, State = d.State }));
    }

    public abstract Task<string> GenerateListQueryAsync(Guid tenantId);
}

