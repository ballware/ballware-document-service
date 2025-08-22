namespace Ballware.Document.Data.Ef.SqlServer;

public class DocumentDbConnectionFactory : IDocumentDbConnectionFactory
{
    public string ConnectionString { get; }

    public DocumentDbConnectionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }
}
