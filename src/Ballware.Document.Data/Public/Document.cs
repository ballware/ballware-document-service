using Ballware.Shared.Data.Public;

namespace Ballware.Document.Data.Public;

public class Document : IEditable
{
    public Guid Id { get; set; }

    public string? DisplayName { get; set; }
    public string? Entity { get; set; }
    public int State { get; set; }
    public string? ReportParameter { get; set; }
}
