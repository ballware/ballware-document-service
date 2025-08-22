namespace Ballware.Document.Data;

public interface IDocumentDbConnectionFactory
{
    string Provider { get; }
    string ConnectionString { get; }
}