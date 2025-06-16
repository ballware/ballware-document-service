using AutoMapper;
using Ballware.Document.Metadata;
using Ballware.Meta.Client;

namespace Ballware.Document.Service.Adapter;

public class MetaServiceDocumentMetadataProvider : IDocumentMetadataProvider
{
    private IMapper Mapper { get; }
    private BallwareMetaClient MetaClient { get; }
    
    public MetaServiceDocumentMetadataProvider(IMapper mapper, BallwareMetaClient metaClient)
    {
        Mapper = mapper;
        MetaClient = metaClient;
    }
    
    public byte[] DocumentBinaryForTenantAndId(Guid tenantId, Guid documentId)
    {
        var document = MetaClient.DocumentMetadataForTenantAndId(tenantId, documentId);

        if (document == null || document.ReportBinary == null)
        {
            throw new InvalidOperationException($"Document with ID {documentId} not found for tenant {tenantId}.");
        }
        
        return document.ReportBinary;
    }

    public IEnumerable<DocumentSelectEntry> DocumentsForTenant(Guid tenantId)
    {
        return Mapper.Map<IEnumerable<DocumentSelectEntry>>(MetaClient.DocumentSelectListForTenant(tenantId));
    }

    public Guid AddDocumentMetadataForTenant(Guid tenantId, Guid userId, string? entity, string displayName, byte[] binary,
        string parameter)
    {
        var document = MetaClient.DocumentNewForTenantAndUser(tenantId);
        
        document.Entity = entity;
        document.DisplayName = displayName;
        document.ReportBinary = binary;
        document.ReportParameter = parameter;
        
        MetaClient.DocumentSaveForTenantBehalfOfUser(tenantId, userId, document);
        
        return document.Id.Value;
    }

    public void UpdateDocumentMetadataForTenantAndId(Guid tenantId, Guid userId, Guid documentId, string? entity,
        string displayName, byte[] binary, string parameter)
    {
        var document = MetaClient.DocumentMetadataForTenantAndId(tenantId, documentId);
        
        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {documentId} not found for tenant {tenantId}.");
        }
        
        document.Entity = entity;
        document.DisplayName = displayName;
        document.ReportBinary = binary;
        document.ReportParameter = parameter;
        
        MetaClient.DocumentSaveForTenantBehalfOfUser(tenantId, userId, document);
    }
}