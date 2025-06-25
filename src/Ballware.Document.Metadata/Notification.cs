namespace Ballware.Document.Metadata;

public class Notification
{
    public required string Name { get; set; }
    public Guid? DocumentId { get; set; }
    public string? Params { get; set; }
}