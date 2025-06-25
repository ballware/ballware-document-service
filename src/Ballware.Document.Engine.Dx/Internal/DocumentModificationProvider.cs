using System.ComponentModel;
using Ballware.Document.Metadata;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;

namespace Ballware.Document.Engine.Dx.Internal;

public class DocumentModificationProvider : IDocumentModificationProvider
{
    private IDocumentMetadataProvider MetadataProvider { get; }
    private IDocumentDatasourceProvider DatasourceProvider { get; }
    
    public DocumentModificationProvider(IDocumentMetadataProvider metadataProvider, IDocumentDatasourceProvider datasourceProvider)
    {
        MetadataProvider = metadataProvider;
        DatasourceProvider = datasourceProvider;
    }
    
    public async Task UpdateDatasourcesAsync(Guid tenantId, Guid userId, Guid documentId)
    {
        var documentBinary = await MetadataProvider.DocumentBinaryForTenantAndIdAsync(tenantId, documentId);

        var report = new XtraReport();
        
        using (var inputStream = new MemoryStream(documentBinary))
        {
            report.LoadLayoutFromXml(inputStream);
        }
        
        var availableDatasources = DatasourceProvider.CreateDatasourcesForTenant(tenantId);
        var documentDatasources = DataSourceManager.GetDataSources(report).ToList();
        
        foreach (var availableDatasource in availableDatasources)
        {   
            var matchingDatasource = documentDatasources.FirstOrDefault(ds => ds is SqlDataSource sqlDs && sqlDs.Name == availableDatasource.Key);
            
            if (matchingDatasource != null)
            {
                DataSourceManager.ReplaceDataSource(report, matchingDatasource, availableDatasource.Value);
                DataSourceManager.AddDataSources(report, availableDatasource.Value as IComponent);    
            } 
            else 
            {
                if (availableDatasource.Value is IComponent newDatasource)
                {
                    DataSourceManager.AddDataSources(report, newDatasource);    
                }
            }
        }
        
        using (var ms = new MemoryStream())
        {
            report.SaveLayoutToXml(ms);

            ms.Position = 0;

            documentBinary = ms.ToArray();
        }
        
        await MetadataProvider.UpdateDocumentBinaryForTenantAndIdAsync(tenantId, userId, documentId, documentBinary);
    }
}