namespace Ballware.Document.Engine.Dx;

public interface IDocumentDatasourceProvider
{
    IDictionary<string, object> CreateDatasourcesForTenant(Guid tenantId);
}