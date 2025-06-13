using Microsoft.Extensions.Logging;

namespace Ballware.Document.Engine.Dx.Internal;

class LoggerService : DevExpress.XtraReports.Web.ClientControls.LoggerService
{
    private ILogger<LoggerService> Log { get; }
    
    public LoggerService(ILogger<LoggerService> log)
    {
        Log = log;
    }
    
    public override void Info(string message)
    {
        Log.LogInformation("{Message}", message);
    }
    public override void Error(Exception exception, string message)
    {
        Log.LogError(exception, "{Message}", message);
    }
}