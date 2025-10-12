namespace Ballware.Document.Engine.Dx;

public interface IDocumentDatasourceProvider
{
    Dictionary<string, object> CreateDatasourcesForTenant(Guid tenantId);
}