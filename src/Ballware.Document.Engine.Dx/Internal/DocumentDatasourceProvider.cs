using Ballware.Document.Metadata;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;

namespace Ballware.Document.Engine.Dx.Internal;

class DocumentDatasourceProvider : IDocumentDatasourceProvider
{
    private IMetaDatasourceProvider MetaDatasourceProvider { get; }
    private ITenantDatasourceProvider TenantDatasourceProvider { get; }
    
    public DocumentDatasourceProvider(IMetaDatasourceProvider metaDatasourceProvider, ITenantDatasourceProvider tenantDatasourceProvider)
    {
        MetaDatasourceProvider = metaDatasourceProvider;
        TenantDatasourceProvider = tenantDatasourceProvider;
    }   
    
    public IDictionary<string, object> CreateDatasourcesForTenant(Guid tenantId)
    {
        var metaDatasourceDefinitions = MetaDatasourceProvider.DatasourceDefinitionsForTenant(tenantId);
        var tenantDatasourceDefinitions = TenantDatasourceProvider.DatasourceDefinitionsForTenant(tenantId);

        var datasources =
            CreateDatasourcesFromDefinitions(metaDatasourceDefinitions.Concat(tenantDatasourceDefinitions));
        
        return datasources;
    }
    
    private static IDictionary<string, object> CreateDatasourcesFromDefinitions(IEnumerable<ReportDatasourceDefinition> definitions)
    {
        var dataSources = new Dictionary<string, object>();

        foreach (var schemaDefinition in definitions
                     .Where(definition => !string.IsNullOrEmpty(definition.ConnectionString) 
                                          && !string.IsNullOrEmpty(definition.Name)))
        {
            var datasource = new SqlDataSource(new CustomStringConnectionParameters(schemaDefinition.ConnectionString))
            {
                Name = schemaDefinition.Name
            };

            foreach (var table in schemaDefinition.Tables ?? [])
            {
                var datasourceQuery = new CustomSqlQuery(table.Name, table.Query);

                datasource.Queries.Add(datasourceQuery);

                if (string.IsNullOrEmpty(table.Name) || table.Relations == null) continue;
                
                foreach (var relation in table.Relations)
                {
                    datasource.Relations.Add(CreateMasterDetailInfoFromRelation(table.Name, relation));
                }
            }

            datasource.RebuildResultSchema();
            dataSources.Add(schemaDefinition.Name, datasource);
        }

        return dataSources;
    }
    
    private static MasterDetailInfo CreateMasterDetailInfoFromRelation(string tableName, ReportDatasourceRelation relation)
    {
        var masterColumns = relation.MasterColumn?.Split(',') ?? [];
        var childColumns = relation.ChildColumn?.Split(',') ?? [];

        var relationColumns = masterColumns.Zip(childColumns,
            (master, child) => new RelationColumnInfo(master, child));

        var datasourceRelation =
            new MasterDetailInfo(tableName, relation.ChildTable, relationColumns)
            {
                Name = relation.Name
            };

        return datasourceRelation;
    }
}