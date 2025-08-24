using Ballware.Shared.Data.Persistables;

namespace Ballware.Document.Data.Persistables;

public class Document : IEntity, IAuditable, ITenantable
{
    public virtual long? Id { get; set; }

    public virtual Guid Uuid { get; set; }

    public virtual Guid TenantId { get; set; }

    public virtual string? DisplayName { get; set; }
    public virtual string? Entity { get; set; }
    public virtual int State { get; set; }
    public virtual string? ReportParameter { get; set; }

    public virtual Guid? CreatorId { get; set; }
    public virtual DateTime? CreateStamp { get; set; }
    public virtual Guid? LastChangerId { get; set; }
    public virtual DateTime? LastChangeStamp { get; set; }
}
