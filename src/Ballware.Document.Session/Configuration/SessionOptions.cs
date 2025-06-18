namespace Ballware.Document.Session.Configuration;

public class SessionOptions
{
    public bool EnableSession { get; set; } = true;
    public string SessionCookieName { get; set; } = ".BallwareDocumentSession";
    public string SessionCacheKeyPrefix { get; set; } = "session:";
    public int SessionCacheExpirationInMinutes { get; set; } = 60;
    public bool SuppressDefaultSessionCookie { get; set; } = true;
}