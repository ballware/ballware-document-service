using Quartz;

namespace Ballware.Document.Jobs.Internal;

public class MailSubscriptionJob : IJob
{
    public static readonly JobKey Key = new JobKey("mail", "document");
    
    public MailSubscriptionJob() {}
    
    public async Task Execute(IJobExecutionContext context)
    {
        // This job is intentionally left empty.
        // It serves as a placeholder for future mail subscription functionality.
        await Task.CompletedTask;
    }
}