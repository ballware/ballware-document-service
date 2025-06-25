using Ballware.Document.Jobs.Configuration;
using Ballware.Document.Jobs.Internal;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.AspNetCore;

namespace Ballware.Document.Jobs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareDocumentBackgroundJobs(this IServiceCollection services, MailOptions mailOptions, TriggerOptions triggerOptions)
    {
        services.AddSingleton(mailOptions);
        
        services.AddQuartz(q =>
        {
            q.AddJob<DocumentUpdateDatasourcesJob>(DocumentUpdateDatasourcesJob.Key, configurator => configurator.StoreDurably());
            q.AddJob<SubscriptionTriggerJob>(SubscriptionTriggerJob.Key, configurator => configurator.StoreDurably());
            q.AddJob<SubscriptionFrequencyJob>(SubscriptionFrequencyJob.Key, configurator => configurator.StoreDurably());
            
            foreach (var trigger in triggerOptions.Active)
            {
                q.AddTrigger(triggerConfigurator =>
                {
                    triggerConfigurator
                        .ForJob(SubscriptionFrequencyJob.Key)
                        .WithIdentity(trigger.Name, "subscription")
                        .WithCronSchedule(trigger.Cron)
                        .UsingJobData("frequency", trigger.Frequency);
                });
            }
        });

        services.AddQuartzServer(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}