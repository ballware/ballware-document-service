using Ballware.Document.Jobs.Configuration;
using Ballware.Document.Jobs.Internal;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.AspNetCore;

namespace Ballware.Document.Jobs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareDocumentBackgroundJobs(this IServiceCollection services, MailOptions mailOptions)
    {
        services.AddSingleton(mailOptions);
        
        services.AddQuartz(q =>
        {
            q.AddJob<DocumentUpdateDatasourcesJob>(DocumentUpdateDatasourcesJob.Key, configurator => configurator.StoreDurably());
            q.AddJob<SubscriptionTriggerJob>(SubscriptionTriggerJob.Key, configurator => configurator.StoreDurably());
        });

        services.AddQuartzServer(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}