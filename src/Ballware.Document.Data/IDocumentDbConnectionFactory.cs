namespace Ballware.Document.Data;

public interface IDocumentDbConnectionFactory
{
    string ConnectionString { get; }
}