namespace Ballware.Document.Data.Ef.SqlServer;

public class DocumentDbConnectionFactory : IDocumentDbConnectionFactory
{
    public string Provider { get; }
    public string ConnectionString { get; }

    public DocumentDbConnectionFactory(string connectionString)
    {
        Provider = "mssql";       
        ConnectionString = connectionString;
    }
}
