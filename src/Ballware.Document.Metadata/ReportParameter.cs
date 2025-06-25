namespace Ballware.Document.Metadata;

public class ReportParameter
{
    public required string Parameter { get; set; }
    public bool Multi { get; set; }
    public string? Value { get; set; }
}