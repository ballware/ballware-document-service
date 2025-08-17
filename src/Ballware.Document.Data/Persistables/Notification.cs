using System.ComponentModel.DataAnnotations;
using Ballware.Shared.Data.Persistables;

namespace Ballware.Document.Data.Persistables;

public class Notification : IEntity, IAuditable, ITenantable
{
    public virtual long? Id { get; set; }

    public virtual Guid Uuid { get; set; }

    public virtual Guid TenantId { get; set; }

    [Required]
    [MaxLength(128)]
    public virtual string? Identifier { get; set; }

    public virtual string? Name { get; set; }
    public virtual Guid? DocumentId { get; set; }
    public virtual int State { get; set; }
    public virtual string? DocumentParams { get; set; }

    public virtual Guid? CreatorId { get; set; }
    public virtual DateTime? CreateStamp { get; set; }
    public virtual Guid? LastChangerId { get; set; }
    public virtual DateTime? LastChangeStamp { get; set; }
}