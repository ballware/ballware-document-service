using Ballware.Shared.Authorization;

namespace Ballware.Document.Metadata;

public class EntityMetadata : IEntityAuthorizationMetadata
{
    public Guid Id { get; set; }
    public string Application { get; set; }
    public string Entity { get; set; }
    public string? RightsCheckScript { get; set; }
    public string? StateAllowedScript { get; set; }
}