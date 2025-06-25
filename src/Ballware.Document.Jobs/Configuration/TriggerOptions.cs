namespace Ballware.Document.Jobs.Configuration;

public class Trigger
{
    public required string Name { get; set; }
    public required string Cron { get; set; }
    public int Frequency { get; set; }
}

public class TriggerOptions
{
    public List<Trigger> Active { get; set; } = new();
}