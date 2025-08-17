using AutoMapper;
using Ballware.Document.Data.Ef.Repository;
using Ballware.Document.Data.Repository;
using Ballware.Document.Data.SelectLists;
using Ballware.Shared.Data.Ef.Repository;
using Ballware.Shared.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Ballware.Document.Data.Ef.Postgres.Repository;

public class DocumentRepository : DocumentBaseRepository
{
    public DocumentRepository(IMapper mapper, IDocumentDbContext dbContext,
        ITenantableRepositoryHook<Public.Document, Persistables.Document>? hook = null)
        : base(mapper, dbContext, hook)
    {
    }

    public override Task<string> GenerateListQueryAsync(Guid tenantId)
    {
        return Task.FromResult(
            $"SELECT uuid AS \"Id\", display_name AS \"Name\", state as \"State\" FROM document WHERE tenant_id='{tenantId}'");
    }
}
