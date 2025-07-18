namespace Ballware.Document.Metadata;

public class ReportDatasourceTable
{
    public string? Name { get; set; }

    public string? Entity { get; set; }

    public string? Query { get; set; }

    public IEnumerable<ReportDatasourceRelation>? Relations { get; set; }
}

public class ReportDatasourceRelation
{
    public string? Name { get; set; }
    public string? ChildTable { get; set; }
    public string? MasterColumn { get; set; }
    public string? ChildColumn { get; set; }
}

public class ReportDatasourceDefinition
{
    public required string Name { get; set; }
    public required string ConnectionString { get; set; }
    public IEnumerable<ReportDatasourceTable>? Tables { get; set; }
}