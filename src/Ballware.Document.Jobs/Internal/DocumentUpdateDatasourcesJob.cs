using Ballware.Document.Metadata;
using Quartz;

namespace Ballware.Document.Jobs.Internal;

public class DocumentUpdateDatasourcesJob : IJob
{
    public static readonly JobKey Key = new JobKey("updatedatasources", "document");
    private IDocumentModificationProvider DocumentModificationProvider { get; }
    
    public DocumentUpdateDatasourcesJob(
        IDocumentModificationProvider documentModificationProvider)
    {
        DocumentModificationProvider = documentModificationProvider;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        var tenantId = context.MergedJobDataMap.GetGuidValue("tenantId");
        var userId = context.MergedJobDataMap.GetGuidValue("userId");
        var documentId = context.MergedJobDataMap.GetGuidValue("documentId");
        
        await DocumentModificationProvider.UpdateDatasourcesAsync(tenantId, userId, documentId);
    }
}