namespace Ballware.Document.Data.Ef.Postgres;

public class DocumentDbConnectionFactory : IDocumentDbConnectionFactory
{
    public string Provider { get; }
    public string ConnectionString { get; }

    public DocumentDbConnectionFactory(string connectionString)
    {
        Provider = "postgres";
        ConnectionString = connectionString;
    }
}
