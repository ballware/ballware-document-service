using Ballware.Shared.Data.Persistables;

namespace Ballware.Document.Data.Persistables;

public class Subscription : IEntity, IAuditable, ITenantable
{
    public virtual long? Id { get; set; }

    public virtual Guid Uuid { get; set; }

    public virtual Guid TenantId { get; set; }
    public virtual Guid UserId { get; set; }
    public virtual string? Mail { get; set; }
    public virtual string? Body { get; set; }
    public virtual bool Attachment { get; set; }
    public virtual string? AttachmentFileName { get; set; }
    public virtual Guid NotificationId { get; set; }
    public virtual int Frequency { get; set; }
    public virtual bool Active { get; set; }
    public virtual DateTime? LastSendStamp { get; set; }
    public virtual string? LastError { get; set; }

    public virtual Guid? CreatorId { get; set; }
    public virtual DateTime? CreateStamp { get; set; }
    public virtual Guid? LastChangerId { get; set; }
    public virtual DateTime? LastChangeStamp { get; set; }
}