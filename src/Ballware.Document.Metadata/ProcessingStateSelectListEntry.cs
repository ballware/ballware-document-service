using System.Diagnostics.CodeAnalysis;

namespace Ballware.Document.Metadata;

public sealed class ProcessingStateSelectListEntry : IEquatable<ProcessingStateSelectListEntry>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int State { get; set; }

    public bool Locked { get; set; }
    public bool Finished { get; set; }

    public bool ReasonRequired { get; set; }
    
    public bool Equals([AllowNull] ProcessingStateSelectListEntry other)
    {
        return Id == other?.Id;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ProcessingStateSelectListEntry);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}