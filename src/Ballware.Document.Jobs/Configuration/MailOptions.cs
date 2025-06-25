namespace Ballware.Document.Jobs.Configuration;

public class MailOptions
{
    public required string FromName { get; set; }
    public required string FromMail { get; set; }
    public required string SmtpHost { get; set; }
    public required int SmtpPort { get; set; }
    public required string SmtpUser { get; set; }
    public required string SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    public int SmtpTimeout { get; set; } = 20000;
    public IEnumerable<Guid> AllowedTenants { get; set; } = new List<Guid>();
}