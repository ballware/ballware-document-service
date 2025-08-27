namespace Ballware.Document.Data.SelectLists;

public class SubscriptionSelectListEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid NotificationId { get; set; }
    public string? Mail { get; set; }
    public bool Active { get; set; }
}
