using Ballware.Document.Jobs.Configuration;
using Ballware.Document.Jobs.Internal;
using Ballware.Shared.Api.Jobs;
using Ballware.Shared.Data.Repository;
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
            var importJobName = "import";
            
            q.AddJob<TenantableImportJob<Data.Public.Document, ITenantableRepository<Data.Public.Document>>>(new JobKey(importJobName, "document"), configurator => configurator.StoreDurably());
            q.AddJob<TenantableImportJob<Data.Public.Notification, ITenantableRepository<Data.Public.Notification>>>(new JobKey(importJobName, "notification"), configurator => configurator.StoreDurably());
            q.AddJob<TenantableImportJob<Data.Public.Subscription, ITenantableRepository<Data.Public.Subscription>>>(new JobKey(importJobName, "subscription"), configurator => configurator.StoreDurably());
            
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