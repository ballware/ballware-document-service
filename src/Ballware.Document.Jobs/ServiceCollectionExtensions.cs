using Ballware.Document.Jobs.Internal;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.AspNetCore;

namespace Ballware.Document.Jobs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBallwareDocumentBackgroundJobs(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.AddJob<MailSubscriptionJob>(MailSubscriptionJob.Key, configurator => configurator.StoreDurably());
        });

        services.AddQuartzServer(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}