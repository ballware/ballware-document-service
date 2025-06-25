using Ballware.Document.Metadata;
using Quartz;

namespace Ballware.Document.Jobs.Internal;

public class SubscriptionFrequencyJob : IJob
{
    public static readonly JobKey Key = new JobKey("frequency", "subscription");
    
    private ISubscriptionMetadataProvider SubscriptionMetadataProvider { get; }
    
    public SubscriptionFrequencyJob(ISubscriptionMetadataProvider subscriptionMetadataProvider)
    {
        SubscriptionMetadataProvider = subscriptionMetadataProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var frequency = context.MergedJobDataMap.GetIntValue("frequency");
        
        var tenants = await SubscriptionMetadataProvider.GetReportAllowedTenantsAsync();

        foreach (var tenantId in tenants.Select(t => t.Id))
        {
            var subscriptions = await SubscriptionMetadataProvider.GetActiveSubscriptionsForTenantFrequencyAsync(tenantId, frequency);
        
            foreach (var subscription in subscriptions)
            {
                var jobDataMap = new JobDataMap
                {
                    { "tenantId", tenantId },
                    { "subscriptionId", subscription.Id }
                };
                
                await context.Scheduler.TriggerJob(SubscriptionTriggerJob.Key, jobDataMap);
            }    
        }
    }
}