using Ballware.Document.Metadata;

namespace Ballware.Document.Api;

public interface IProcessingStateProvider
{
    Task<IEnumerable<ProcessingStateSelectListEntry>> SelectListPossibleSuccessorsForEntityAsync(Guid tenantId,
        string entity, int state);
}