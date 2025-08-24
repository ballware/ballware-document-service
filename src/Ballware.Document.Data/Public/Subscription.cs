using Ballware.Shared.Data.Public;

namespace Ballware.Document.Data.Public;

public class Subscription : IEditable
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public string? Mail { get; set; }
    public string? Body { get; set; }
    public bool Attachment { get; set; }
    public string? AttachmentFileName { get; set; }
    public Guid NotificationId { get; set; }
    public int Frequency { get; set; }
    public bool Active { get; set; }
    public DateTime? LastSendStamp { get; set; }
    public string? LastError { get; set; }
}