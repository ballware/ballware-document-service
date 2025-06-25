namespace Ballware.Document.Metadata;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid? NotificationId { get; set; }
    public string? Mail { get; set; }
    public string? Body { get; set; }
    public bool Attachment { get; set; }
    public string? AttachmentFileName { get; set; }
}