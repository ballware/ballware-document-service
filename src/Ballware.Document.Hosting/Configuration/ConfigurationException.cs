namespace Ballware.Document.Hosting.Configuration;

public class ConfigurationException(string message, Exception? innerException = null)
    : Exception(message, innerException);