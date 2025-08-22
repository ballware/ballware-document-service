namespace Ballware.Document.Data.Ef.Postgres;

public class DocumentDbConnectionFactory : IDocumentDbConnectionFactory
{
    public string ConnectionString { get; }

    public DocumentDbConnectionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }
}
